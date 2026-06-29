using System;
using System.Buffers.Text;
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

        private static readonly JsonSerializerOptions s_jsonOptions = new()
        {
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        private readonly RSA _rsa;
        private readonly X509Certificate2 _certificate;
        private readonly string _publicKeyPem;
        private readonly string _certificateBase64;
        private readonly string _keyId;

        public RsaEsdcService(IConfiguration configuration)
        {
            _rsa = RSA.Create(2048);
            string privateKeyPem = configuration?["Dpp:Esdc:PrivateKeyPem"];
            if (!string.IsNullOrWhiteSpace(privateKeyPem))
            {
                _rsa.ImportFromPem(privateKeyPem);
            }

            _publicKeyPem = _rsa.ExportSubjectPublicKeyInfoPem();

            string certificatePem = configuration?["Dpp:Esdc:CertificatePem"];
            if (!string.IsNullOrWhiteSpace(certificatePem))
            {
                _certificate = X509Certificate2.CreateFromPem(certificatePem);
                _certificateBase64 = Convert.ToBase64String(_certificate.RawData);
                _keyId = _certificate.Thumbprint;
            }
            else
            {
                // No certificate: derive a stable key id from the public key so verifiers can still
                // select the matching trust anchor.
                _keyId = Convert.ToHexString(SHA256.HashData(_rsa.ExportSubjectPublicKeyInfo()));
            }
        }

        public ElectronicSignedDataConstruct Issue(DigitalProductPassport dpp)
        {
            ArgumentNullException.ThrowIfNull(dpp);

            var credential = new VerifiableCredential
            {
                Id = $"urn:uuid:{Guid.NewGuid()}",
                Type = new() { "VerifiableCredential", CredentialType },
                Issuer = dpp.EconomicOperatorId,
                ValidFrom = DateTimeOffset.UtcNow,
                CredentialSubject = new DppCredentialSubject
                {
                    Id = dpp.UniqueProductIdentifier,
                    DigitalProductPassport = dpp
                }
            };

            var header = new JwsHeader
            {
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

            return new ElectronicSignedDataConstruct
            {
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
                using RSA verifier = ResolveVerificationKey(esdc, parts[0]);
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

                // Confirm the payload is actually a Verifiable Credential.
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
        }

        // Resolves the public key to verify the JWS with: prefer the certificate chain embedded in the
        // JWS header (x5c), then the certificate or public key carried by the ESDC envelope.
        private static RSA ResolveVerificationKey(ElectronicSignedDataConstruct esdc, string encodedHeader)
        {
            using (var header = JsonDocument.Parse(Base64Url.DecodeFromChars(encodedHeader)))
            {
                if (header.RootElement.TryGetProperty("x5c", out JsonElement x5c)
                    && x5c.ValueKind == JsonValueKind.Array
                    && x5c.GetArrayLength() > 0)
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
                rsa.ImportFromPem(esdc.PublicKey);
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
            _rsa.Dispose();
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
