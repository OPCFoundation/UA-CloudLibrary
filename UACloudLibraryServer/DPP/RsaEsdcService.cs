using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Opc.Ua.Cloud.Library.Models;

namespace Opc.Ua.Cloud.Library
{
    /// <summary>
    /// Issues and verifies the DPP ESDC as a W3C Verifiable Credential (VC Data Model 2.0) secured with
    /// an enveloped JWS (<c>application/vc+jwt</c>) using RSA (RS256), per the W3C "Securing Verifiable
    /// Credentials using JOSE and COSE" recommendation — one of the ESDC formats recognized by
    /// EN 18246 (Annex B.5). The signing key is loaded from <c>Dpp:Esdc:PrivateKeyPem</c>; an optional
    /// issuer certificate (<c>Dpp:Esdc:CertificatePem</c>) is embedded in the JWS header (<c>x5c</c>) so
    /// verifiers can check the issuer's authority against a trusted list. When no key is configured an
    /// ephemeral key is generated so the service works out of the box. The resulting VC-JWT is
    /// self-contained and independently verifiable, free of charge and without contacting the issuer
    /// (Annex A.3, §4.7).
    /// </summary>
    /// <remarks>
    /// The JWS establishes data integrity and non-repudiation relative to the signing key. Establishing
    /// that the issuer's certificate belongs to an accredited economic operator (validation against an EU
    /// trusted list / governance framework, Annex A.3 fourth bullet) is a deployment responsibility and
    /// is intentionally out of scope of this service.
    /// </remarks>
    public sealed class RsaEsdcService : IEsdcService, IDisposable
    {
        private const string Algorithm = "RS256";
        private const string MediaType = "application/vc+jwt";
        private const string JwsType = "vc+jwt";
        private const string CredentialType = "DigitalProductPassportCredential";

        private static readonly JsonSerializerOptions s_jsonOptions = new() {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly RSA _rsa;
        private readonly X509Certificate2 _certificate;
        private readonly string _publicKeyPem;
        private readonly string _certificateBase64;
        private readonly string _keyId;

        // Keys accepted by Verify, each bound to the issuer it is authorized to speak for. Never
        // populated from a submitted ESDC.
        private readonly List<TrustAnchor> _trustedKeys = new();

        /// <summary>
        /// A public key together with the issuer it is authorized to sign for.
        /// </summary>
        /// <remarks>
        /// The binding is the point: a bare set of trusted keys establishes only that *someone*
        /// trusted signed the credential, so any trusted peer could issue one naming a different
        /// economic operator and still verify.
        /// </remarks>
        private sealed class TrustAnchor
        {
            internal TrustAnchor(RSA key, string issuer, string keyId, bool isOwnKey)
            {
                Key = key;
                Issuer = issuer;
                KeyId = keyId;
                IsOwnKey = isOwnKey;
            }

            internal RSA Key { get; }

            /// <summary>Issuer this key may sign for; null means unbound (legacy configuration).</summary>
            internal string Issuer { get; }

            /// <summary>Optional pinned JWS <c>kid</c>.</summary>
            internal string KeyId { get; }

            /// <summary>True for this server's own signing key.</summary>
            internal bool IsOwnKey { get; }
        }

        /// <summary>
        /// True when <paramref name="anchor"/> is authorized to vouch for the signed issuer.
        /// </summary>
        /// <remarks>
        /// An issuer-bound anchor must match the signed <c>issuer</c>, and its pinned <c>kid</c>
        /// when one is configured. Unbound anchors (this server's own key, and the legacy flat
        /// list) are only accepted when the signed issuer is absent or is not claimed by any bound
        /// anchor - so adding a bound anchor for an operator stops an unbound key from
        /// impersonating it.
        /// </remarks>
        private bool AnchorMayVouchFor(TrustAnchor anchor, string signedIssuer, string signedKeyId)
        {
            if (anchor.Issuer is not null)
            {
                if (!string.Equals(anchor.Issuer, signedIssuer, StringComparison.Ordinal))
                {
                    return false;
                }

                // A pinned key id, when configured, must match the signed header.
                return string.IsNullOrEmpty(anchor.KeyId)
                    || string.Equals(anchor.KeyId, signedKeyId, StringComparison.Ordinal);
            }

            // Unbound anchor: refuse if some bound anchor already claims this issuer.
            if (!string.IsNullOrEmpty(signedIssuer)
                && _trustedKeys.Any(a => a.Issuer is not null && string.Equals(a.Issuer, signedIssuer, StringComparison.Ordinal)))
            {
                return false;
            }

            return true;
        }

        private static RSA ImportAnchorKey(string publicKeyPem, string configurationPath)
        {
            var anchor = RSA.Create();
            try
            {
                anchor.ImportFromPem(publicKeyPem);
                return anchor;
            }
            catch (ArgumentException)
            {
                // A malformed trust anchor must not silently widen or narrow trust.
                anchor.Dispose();
                throw new InvalidOperationException(
                    $"Configured ESDC trust anchor '{configurationPath}' is not a valid public key PEM.");
            }
        }

        /// <summary>
        /// Creates the service using an explicitly resolved signing key. This is the production path:
        /// <see cref="IEsdcSigningKeyProvider"/> has already decided whether the key came from
        /// configuration, from the database, or had to be generated, so the key is stable across
        /// restarts and identical on every instance.
        /// </summary>
        public RsaEsdcService(IConfiguration configuration, string privateKeyPem)
            : this(configuration, privateKeyPem, requireResolvedKey: true)
        {
        }

        /// <summary>
        /// Creates the service from configuration alone, generating an ephemeral key when none is
        /// configured. The ephemeral key lives only as long as this instance, so ESDCs it signs stop
        /// verifying once the process exits; use the <see cref="IEsdcSigningKeyProvider"/> overload
        /// outside of tests.
        /// </summary>
        public RsaEsdcService(IConfiguration configuration)
            : this(configuration, null, requireResolvedKey: false)
        {
        }

        private RsaEsdcService(IConfiguration configuration, string privateKeyPem, bool requireResolvedKey)
        {
            _rsa = RSA.Create(2048);

            // Prefer the resolved key; fall back to configuration so the config-only constructor and
            // any direct configuration binding keep working.
            string keyPem = !string.IsNullOrWhiteSpace(privateKeyPem)
                ? privateKeyPem
                : configuration?["Dpp:Esdc:PrivateKeyPem"];

            if (!string.IsNullOrWhiteSpace(keyPem))
            {
                _rsa.ImportFromPem(keyPem);
            }
            else if (requireResolvedKey)
            {
                throw new InvalidOperationException(
                    "No ESDC signing key was resolved. The signing key must be stable, so the service refuses to fall back to an ephemeral key here.");
            }

            _publicKeyPem = _rsa.ExportSubjectPublicKeyInfoPem();

            string certificatePem = configuration?["Dpp:Esdc:CertificatePem"];
            if (!string.IsNullOrWhiteSpace(certificatePem))
            {
                _certificate = X509Certificate2.CreateFromPem(certificatePem);

                // The certificate is published as x5c and its thumbprint becomes kid, so verifiers
                // are told "this certificate's key signed this credential". If it belongs to a
                // different key that claim is false: the advertised certificate cannot verify the
                // signature, which defeats the certificate-backed independent verification the
                // certificate exists to provide. Refuse to start rather than issue credentials that
                // misrepresent their own signer.
                EnsureCertificateMatchesSigningKey(_certificate, _rsa);

                _certificateBase64 = Convert.ToBase64String(_certificate.RawData);
                _keyId = _certificate.Thumbprint;
            }
            else
            {
                // No certificate: derive a stable key id from the public key so verifiers can still
                // select the matching trust anchor.
                _keyId = Convert.ToHexString(SHA256.HashData(_rsa.ExportSubjectPublicKeyInfo()));
            }

            // Our own key always verifies what we issue, bound to this server as its own issuer.
            _trustedKeys.Add(new TrustAnchor(_rsa, issuer: null, keyId: _keyId, isOwnKey: true));

            // Additional trust anchors let this instance verify ESDCs issued by peer operators.
            //
            // Preferred form binds each key to the issuer it is authorized to sign for:
            //   Dpp:Esdc:TrustedIssuers:0:Issuer        - economic operator id
            //   Dpp:Esdc:TrustedIssuers:0:PublicKeyPem  - SubjectPublicKeyInfo PEM
            //   Dpp:Esdc:TrustedIssuers:0:KeyId         - optional, pins the JWS 'kid'
            //
            // Without that binding any trusted peer could sign a credential claiming a different
            // operator as issuer and still verify, because the key set alone says nothing about who
            // each key speaks for.
            IConfigurationSection boundIssuers = configuration?.GetSection("Dpp:Esdc:TrustedIssuers");
            if (boundIssuers is not null)
            {
                foreach (IConfigurationSection child in boundIssuers.GetChildren())
                {
                    string issuer = child["Issuer"];
                    string publicKeyPem = child["PublicKeyPem"];
                    string keyId = child["KeyId"];

                    if (string.IsNullOrWhiteSpace(publicKeyPem))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(issuer))
                    {
                        throw new InvalidOperationException(
                            $"Configured ESDC trust anchor '{child.Path}' has no 'Issuer'. An anchor without an issuer would authorize its holder to sign as any operator.");
                    }

                    _trustedKeys.Add(new TrustAnchor(ImportAnchorKey(publicKeyPem, child.Path), issuer, keyId, isOwnKey: false));
                }
            }

            // Legacy flat list: keys with no issuer binding. Retained so existing deployments keep
            // working, but they are only accepted for credentials this server itself issued - an
            // unbound key must not be able to vouch for an arbitrary issuer.
            IConfigurationSection trusted = configuration?.GetSection("Dpp:Esdc:TrustedPublicKeysPem");
            if (trusted is not null)
            {
                foreach (IConfigurationSection child in trusted.GetChildren())
                {
                    if (string.IsNullOrWhiteSpace(child.Value))
                    {
                        continue;
                    }

                    _trustedKeys.Add(new TrustAnchor(ImportAnchorKey(child.Value, child.Path), issuer: null, keyId: null, isOwnKey: false));
                }
            }
        }

        /// <summary>
        /// Throws unless <paramref name="certificate"/> carries the public half of
        /// <paramref name="signingKey"/>. Compares the exported SubjectPublicKeyInfo, which covers
        /// the algorithm and all parameters, rather than the modulus alone.
        /// </summary>
        private static void EnsureCertificateMatchesSigningKey(X509Certificate2 certificate, RSA signingKey)
        {
            using RSA certificateKey = certificate.GetRSAPublicKey();
            if (certificateKey is null)
            {
                throw new InvalidOperationException(
                    "Dpp:Esdc:CertificatePem does not contain an RSA public key, so it cannot correspond to the ESDC signing key.");
            }

            byte[] fromCertificate = certificateKey.ExportSubjectPublicKeyInfo();
            byte[] fromSigningKey = signingKey.ExportSubjectPublicKeyInfo();

            if (!CryptographicOperations.FixedTimeEquals(fromCertificate, fromSigningKey))
            {
                throw new InvalidOperationException(
                    "Dpp:Esdc:CertificatePem does not match the ESDC signing key. The certificate is published as 'x5c' and its thumbprint as 'kid', " +
                    "so a mismatched certificate would tell verifiers that a key which cannot verify the signature produced it. " +
                    "Supply the certificate issued for Dpp:Esdc:PrivateKeyPem, or remove the certificate setting.");
            }
        }

        public ElectronicSignedDataConstruct Issue(DigitalProductPassport dpp)
        {
            ArgumentNullException.ThrowIfNull(dpp);

            var credential = new VerifiableCredential {
                Id = $"urn:uuid:{Guid.NewGuid()}",
                Type = new() { "VerifiableCredential", CredentialType },
                Issuer = dpp.EconomicOperatorId,
                ValidFrom = DateTimeOffset.UtcNow,
                CredentialSubject = new DppCredentialSubject {
                    Id = dpp.UniqueProductIdentifier,
                    DigitalProductPassport = dpp
                }
            };

            var header = new JwsHeader {
                Algorithm = Algorithm,
                Type = JwsType,
                KeyId = _keyId,
                X5c = _certificateBase64 is null ? null : new[] { _certificateBase64 }
            };

            string encodedHeader = Base64Url.EncodeToString(JsonUtf8(header));
            string encodedPayload = Base64Url.EncodeToString(JsonUtf8(credential));
            string signingInput = encodedHeader + "." + encodedPayload;
            byte[] signature = _rsa.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            string jwt = signingInput + "." + Base64Url.EncodeToString(signature);

            return new ElectronicSignedDataConstruct {
                Issuer = credential.Issuer,
                Subject = credential.CredentialSubject.Id,
                IssuedAt = credential.ValidFrom,
                KeyId = _keyId,
                Format = MediaType,
                SignatureAlgorithm = Algorithm,
                VerifiableCredentialJwt = jwt,
                PublicKey = _publicKeyPem,
                Certificate = _certificateBase64
            };
        }

        /// <summary>
        /// Verifies that the ESDC was signed by a trusted key and that its envelope metadata matches
        /// the signed credential. The verification key is taken from configuration (or this instance's
        /// own key), never from the ESDC itself: trusting an embedded key would only prove the
        /// document is internally consistent, which any attacker can arrange with a fresh key pair.
        /// </summary>
        public bool Verify(ElectronicSignedDataConstruct esdc)
        {
            if (esdc?.VerifiableCredentialJwt is null)
            {
                return false;
            }

            string[] parts = esdc.VerifiableCredentialJwt.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            try
            {
                byte[] signature = Base64Url.DecodeFromChars(parts[2]);
                byte[] signingInput = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);

                using var header = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[0]));
                using var payload = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));

                // Both halves are attacker-controlled and only guaranteed to be *valid JSON*, not JSON
                // objects. TryGetProperty throws InvalidOperationException on any other root kind (an
                // array, say), which would turn this boolean API into an exception on malformed input.
                if (header.RootElement.ValueKind != JsonValueKind.Object
                    || payload.RootElement.ValueKind != JsonValueKind.Object)
                {
                    return false;
                }

                // Confirm the payload is actually a Verifiable Credential.
                if (!IsVerifiableCredential(payload.RootElement))
                {
                    return false;
                }

                // Select the anchor from the *signed* issuer before checking the signature, rather
                // than accepting whichever trusted key happens to verify. Trying every key would
                // only establish that some trusted party signed this, so any trusted peer could
                // issue a credential naming a different economic operator and still pass.
                string signedIssuer = payload.RootElement.TryGetProperty("issuer", out JsonElement issuerElement)
                    && issuerElement.ValueKind == JsonValueKind.String
                        ? issuerElement.GetString()
                        : null;

                string signedKeyId = header.RootElement.TryGetProperty("kid", out JsonElement kidElement)
                    && kidElement.ValueKind == JsonValueKind.String
                        ? kidElement.GetString()
                        : null;

                bool signatureValid = false;
                foreach (TrustAnchor anchor in _trustedKeys)
                {
                    if (!AnchorMayVouchFor(anchor, signedIssuer, signedKeyId))
                    {
                        continue;
                    }

                    if (anchor.Key.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                    {
                        signatureValid = true;
                        break;
                    }
                }

                if (!signatureValid)
                {
                    return false;
                }

                // The envelope duplicates metadata that is also inside the signed credential. Only the
                // signed copy is authenticated, so the unsigned copy must be proven to agree with it -
                // otherwise a caller could relabel the issuer or product and still verify.
                return EnvelopeMatchesSignedContent(esdc, header.RootElement, payload.RootElement);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                return false;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        /// <summary>
        /// Checks only that the JWS is internally consistent with a key carried inside it. This proves
        /// the document was not altered after signing, but says nothing about who signed it, so it must
        /// not be used to decide whether an ESDC is authentic. Provided separately because independent
        /// verifiers without access to the issuer's trust anchors may still want an integrity check.
        /// </summary>
        public static bool VerifyIntegrityOnly(ElectronicSignedDataConstruct esdc)
        {
            if (esdc?.VerifiableCredentialJwt is null)
            {
                return false;
            }

            string[] parts = esdc.VerifiableCredentialJwt.Split('.');
            if (parts.Length != 3)
            {
                return false;
            }

            try
            {
                using RSA verifier = ResolveEmbeddedKey(esdc, parts[0]);
                if (verifier is null)
                {
                    return false;
                }

                byte[] signature = Base64Url.DecodeFromChars(parts[2]);
                byte[] signingInput = Encoding.UTF8.GetBytes(parts[0] + "." + parts[1]);
                if (!verifier.VerifyData(signingInput, signature, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1))
                {
                    return false;
                }

                using var payload = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
                return IsVerifiableCredential(payload.RootElement);
            }
            catch (FormatException)
            {
                return false;
            }
            catch (JsonException)
            {
                return false;
            }
            catch (CryptographicException)
            {
                return false;
            }
            catch (ArgumentException)
            {
                // Malformed embedded key material (bad PEM, bad certificate bytes).
                return false;
            }
            catch (InvalidOperationException)
            {
                // Defence in depth: a JSON element accessed as the wrong kind. The checks above should
                // prevent this, but this is a boolean API over untrusted input and must not throw.
                return false;
            }
        }

        // Requires every envelope field that is also covered by the signature to match its signed value.
        private static bool EnvelopeMatchesSignedContent(ElectronicSignedDataConstruct esdc, JsonElement header, JsonElement credential)
        {
            if (!string.IsNullOrEmpty(esdc.Format) && !string.Equals(esdc.Format, MediaType, StringComparison.Ordinal))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(esdc.SignatureAlgorithm) && !string.Equals(esdc.SignatureAlgorithm, Algorithm, StringComparison.Ordinal))
            {
                return false;
            }

            if (!MatchesHeaderString(header, "alg", Algorithm) || !MatchesHeaderString(header, "typ", JwsType))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(esdc.KeyId) && !MatchesHeaderString(header, "kid", esdc.KeyId))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(esdc.Issuer) && !MatchesCredentialString(credential, "issuer", esdc.Issuer))
            {
                return false;
            }

            if (!string.IsNullOrEmpty(esdc.Subject)
                && (!credential.TryGetProperty("credentialSubject", out JsonElement subject)
                    || !MatchesCredentialString(subject, "id", esdc.Subject)))
            {
                return false;
            }

            // A supplied IssuedAt must be backed by a signed value. Treating an absent or wrongly
            // typed validFrom as "nothing to compare" would let an envelope carry an arbitrary
            // timestamp that no signed field contradicts, which is exactly the metadata-binding gap
            // this method exists to close - so the signed value is required, not merely checked when
            // conveniently present.
            if (esdc.IssuedAt != default)
            {
                if (!credential.TryGetProperty("validFrom", out JsonElement validFrom)
                    || validFrom.ValueKind != JsonValueKind.String
                    || !DateTimeOffset.TryParse(
                            validFrom.GetString(),
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.RoundtripKind,
                            out DateTimeOffset signedIssuedAt)
                    || signedIssuedAt.ToUniversalTime() != esdc.IssuedAt.ToUniversalTime())
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesHeaderString(JsonElement header, string property, string expected) =>
            header.TryGetProperty(property, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
            && string.Equals(value.GetString(), expected, StringComparison.Ordinal);

        private static bool MatchesCredentialString(JsonElement element, string property, string expected) =>
            element.TryGetProperty(property, out JsonElement value)
            && value.ValueKind == JsonValueKind.String
            && string.Equals(value.GetString(), expected, StringComparison.Ordinal);

        // Reads the key embedded in the document. Only ever used for the explicitly integrity-only
        // check; never for deciding authenticity.
        private static RSA ResolveEmbeddedKey(ElectronicSignedDataConstruct esdc, string encodedHeader)
        {
            using (var header = JsonDocument.Parse(Base64Url.DecodeFromChars(encodedHeader)))
            {
                // The header is attacker-supplied: it need not be an object, and x5c[0] need not be a
                // string. GetString() throws InvalidOperationException on any other element kind, which
                // would escape the boolean verification API rather than being reported as "invalid".
                if (header.RootElement.ValueKind == JsonValueKind.Object
                    && header.RootElement.TryGetProperty("x5c", out JsonElement x5c)
                    && x5c.ValueKind == JsonValueKind.Array
                    && x5c.GetArrayLength() > 0
                    && x5c[0].ValueKind == JsonValueKind.String)
                {
                    using X509Certificate2 headerCert = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(x5c[0].GetString()));
                    return headerCert.GetRSAPublicKey();
                }
            }

            if (!string.IsNullOrEmpty(esdc.Certificate))
            {
                using X509Certificate2 certificate = X509CertificateLoader.LoadCertificate(Convert.FromBase64String(esdc.Certificate));
                return certificate.GetRSAPublicKey();
            }

            if (!string.IsNullOrEmpty(esdc.PublicKey))
            {
                var rsa = RSA.Create();
                try
                {
                    // ImportFromPem throws ArgumentException on malformed PEM, which the callers do not
                    // catch. Dispose the instance and let the caller treat this as unverifiable.
                    rsa.ImportFromPem(esdc.PublicKey);
                }
                catch (ArgumentException)
                {
                    rsa.Dispose();
                    return null;
                }
                catch (CryptographicException)
                {
                    rsa.Dispose();
                    return null;
                }

                return rsa;
            }

            return null;
        }

        private static bool IsVerifiableCredential(JsonElement credential)
        {
            if (credential.ValueKind != JsonValueKind.Object
                || !credential.TryGetProperty("type", out JsonElement type)
                || !credential.TryGetProperty("credentialSubject", out _))
            {
                return false;
            }

            if (type.ValueKind == JsonValueKind.String)
            {
                return string.Equals(type.GetString(), "VerifiableCredential", StringComparison.Ordinal);
            }

            if (type.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement item in type.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String && string.Equals(item.GetString(), "VerifiableCredential", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static byte[] JsonUtf8<T>(T value) =>
            JsonSerializer.SerializeToUtf8Bytes(value, s_jsonOptions);

        public void Dispose()
        {
            foreach (TrustAnchor trusted in _trustedKeys)
            {
                // _rsa is in this list; disposing it here covers it once.
                trusted.Key.Dispose();
            }

            _trustedKeys.Clear();
            _certificate?.Dispose();
        }

        // Minimal JOSE protected header for the enveloped VC-JWT.
        private sealed class JwsHeader
        {
            [JsonPropertyName("alg")]
            public string Algorithm { get; init; }

            [JsonPropertyName("typ")]
            public string Type { get; init; }

            [JsonPropertyName("kid")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string KeyId { get; init; }

            [JsonPropertyName("x5c")]
            [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
            public string[] X5c { get; init; }
        }
    }
}
