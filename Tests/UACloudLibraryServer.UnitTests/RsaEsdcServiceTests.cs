using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Json;

using Microsoft.Extensions.Configuration;

using Opc.Ua.Cloud.Library;
using Opc.Ua.Cloud.Library.Models;

using Xunit;

namespace UACloudLibraryServer.UnitTests
{
    /// <summary>
    /// Unit tests for <see cref="RsaEsdcService"/>: the ESDC is a W3C Verifiable Credential (VC Data
    /// Model 2.0) secured as an enveloped JWS (application/vc+jwt). The credential structure is correct,
    /// it verifies, tampering breaks verification, and it is independently verifiable by a different
    /// service instance (EN 18246 Annex A.3 / B.5, §4.7). Ephemeral key, no config.
    /// </summary>
    public class RsaEsdcServiceTests
    {
        private static DigitalProductPassport SampleDpp() => new()
        {
            DigitalProductPassportId = "dpp-1",
            UniqueProductIdentifier = "prod-1",
            DppSchemaVersion = "1.0",
            DppStatus = "active",
            LastUpdate = DateTimeOffset.UtcNow,
            EconomicOperatorId = "EO-1"
        };

        private static JsonDocument DecodePayload(string jwt)
        {
            string[] parts = jwt.Split('.');
            return JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));
        }

        [Fact]
        public void Issue_ProducesVerifiableCredentialJwt()
        {
            using var service = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = service.Issue(SampleDpp());

            Assert.Equal("EO-1", esdc.Issuer);
            Assert.Equal("prod-1", esdc.Subject);
            Assert.Equal("RS256", esdc.SignatureAlgorithm);
            Assert.Equal("application/vc+jwt", esdc.Format);
            Assert.Equal(3, esdc.VerifiableCredentialJwt.Split('.').Length);
            Assert.Contains("BEGIN PUBLIC KEY", esdc.PublicKey);

            using JsonDocument payload = DecodePayload(esdc.VerifiableCredentialJwt);
            JsonElement vc = payload.RootElement;

            Assert.Equal("https://www.w3.org/ns/credentials/v2", vc.GetProperty("@context")[0].GetString());
            Assert.Equal("VerifiableCredential", vc.GetProperty("type")[0].GetString());
            Assert.Equal("DigitalProductPassportCredential", vc.GetProperty("type")[1].GetString());
            Assert.Equal("EO-1", vc.GetProperty("issuer").GetString());
            Assert.Equal("prod-1", vc.GetProperty("credentialSubject").GetProperty("id").GetString());
            Assert.Equal("dpp-1", vc.GetProperty("credentialSubject").GetProperty("digitalProductPassport").GetProperty("digitalProductPassportId").GetString());

            Assert.True(service.Verify(esdc));
        }

        [Fact]
        public void Verify_FailsWhenCredentialTampered()
        {
            using var service = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = service.Issue(SampleDpp());

            string[] parts = esdc.VerifiableCredentialJwt.Split('.');
            string payloadJson = Encoding.UTF8.GetString(Base64Url.DecodeFromChars(parts[1]));
            string tamperedJson = payloadJson.Replace("prod-1", "prod-2", StringComparison.Ordinal);
            string tamperedJwt = parts[0] + "." + Base64Url.EncodeToString(Encoding.UTF8.GetBytes(tamperedJson)) + "." + parts[2];

            var tampered = new ElectronicSignedDataConstruct
            {
                Issuer = esdc.Issuer,
                Subject = esdc.Subject,
                IssuedAt = esdc.IssuedAt,
                KeyId = esdc.KeyId,
                Format = esdc.Format,
                SignatureAlgorithm = esdc.SignatureAlgorithm,
                VerifiableCredentialJwt = tamperedJwt,
                PublicKey = esdc.PublicKey
            };

            Assert.False(service.Verify(tampered));
        }

        [Fact]
        public void Verify_RejectsCredentialFromUntrustedIssuer()
        {
            using var issuer = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = issuer.Issue(SampleDpp());

            // A different instance has its own ephemeral key and no configured trust anchors, so it
            // must NOT accept this credential. Accepting it would mean trusting the key the document
            // carries, which proves only self-consistency - any forger can supply a matching key.
            using var independentVerifier = new RsaEsdcService(null);
            Assert.False(independentVerifier.Verify(esdc));

            // The signature itself is intact, which the integrity-only check confirms. That check is
            // deliberately separate so it cannot be mistaken for proof of authenticity.
            Assert.True(RsaEsdcService.VerifyIntegrityOnly(esdc));
        }

        // These are boolean APIs over entirely attacker-controlled input, so malformed material must
        // come back as "false" rather than as an exception escaping to the caller.
        [Theory]
        // Header is valid JSON but an array, not an object: TryGetProperty("kid") would throw.
        [InlineData("[]")]
        [InlineData("[1,2,3]")]
        // Header is a bare JSON scalar.
        [InlineData("\"a string\"")]
        [InlineData("42")]
        [InlineData("null")]
        public void Verify_ReturnsFalseForNonObjectHeader(string headerJson)
        {
            using var issuer = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = issuer.Issue(SampleDpp());

            string[] parts = esdc.VerifiableCredentialJwt.Split('.');
            string forgedJwt = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(headerJson))
                + "." + parts[1] + "." + parts[2];

            var malformed = new ElectronicSignedDataConstruct {
                VerifiableCredentialJwt = forgedJwt,
                PublicKey = esdc.PublicKey
            };

            using var verifier = new RsaEsdcService(null);
            Assert.False(verifier.Verify(malformed));
            Assert.False(RsaEsdcService.VerifyIntegrityOnly(malformed));
        }

        [Theory]
        // x5c present but its first element is not a string: GetString() would throw.
        [InlineData("{\"alg\":\"RS256\",\"x5c\":[123]}")]
        [InlineData("{\"alg\":\"RS256\",\"x5c\":[{}]}")]
        [InlineData("{\"alg\":\"RS256\",\"x5c\":[null]}")]
        public void VerifyIntegrityOnly_ReturnsFalseForNonStringX5c(string headerJson)
        {
            using var issuer = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = issuer.Issue(SampleDpp());

            string[] parts = esdc.VerifiableCredentialJwt.Split('.');
            string forgedJwt = Base64Url.EncodeToString(Encoding.UTF8.GetBytes(headerJson))
                + "." + parts[1] + "." + parts[2];

            Assert.False(RsaEsdcService.VerifyIntegrityOnly(new ElectronicSignedDataConstruct {
                VerifiableCredentialJwt = forgedJwt
            }));
        }

        [Theory]
        [InlineData("not a pem at all")]
        [InlineData("-----BEGIN PUBLIC KEY-----\nnot base64!!!\n-----END PUBLIC KEY-----")]
        [InlineData("-----BEGIN PUBLIC KEY-----\n-----END PUBLIC KEY-----")]
        public void VerifyIntegrityOnly_ReturnsFalseForMalformedPublicKeyPem(string pem)
        {
            using var issuer = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = issuer.Issue(SampleDpp());

            // ImportFromPem throws ArgumentException on malformed PEM; it must not escape.
            Assert.False(RsaEsdcService.VerifyIntegrityOnly(new ElectronicSignedDataConstruct {
                VerifiableCredentialJwt = esdc.VerifiableCredentialJwt,
                PublicKey = pem
            }));
        }

        [Fact]
        public void Verify_RejectsSelfSignedForgery()
        {
            // A forger mints their own key pair, signs a credential of their choosing, and embeds the
            // matching public key in the envelope. This is the attack the trusted-key check prevents.
            using var forger = new RsaEsdcService(null);
            ElectronicSignedDataConstruct forged = forger.Issue(SampleDpp());

            using var legitimate = new RsaEsdcService(null);
            Assert.False(legitimate.Verify(forged));
        }

        [Fact]
        public void Verify_AcceptsCredentialFromConfiguredTrustAnchor()
        {
            using var issuer = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = issuer.Issue(SampleDpp());

            // Configuring the issuer's public key as a trust anchor is the supported way for a peer to
            // verify another operator's ESDC.
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string> {
                    ["Dpp:Esdc:TrustedPublicKeysPem:0"] = esdc.PublicKey
                })
                .Build();

            using var verifier = new RsaEsdcService(configuration);
            Assert.True(verifier.Verify(esdc));
        }

        [Fact]
        public void Verify_RejectsEnvelopeMetadataThatContradictsSignedCredential()
        {
            using var service = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = service.Issue(SampleDpp());

            // Envelope fields are not covered by the signature, so a caller can rewrite them. Consumers
            // that trust the envelope would be misled unless verification binds it to the signed copy.
            var relabelled = new ElectronicSignedDataConstruct {
                Issuer = "EO-IMPOSTER",
                Subject = esdc.Subject,
                IssuedAt = esdc.IssuedAt,
                KeyId = esdc.KeyId,
                Format = esdc.Format,
                SignatureAlgorithm = esdc.SignatureAlgorithm,
                VerifiableCredentialJwt = esdc.VerifiableCredentialJwt,
                PublicKey = esdc.PublicKey,
                Certificate = esdc.Certificate
            };

            Assert.False(service.Verify(relabelled));

            var reSubjected = new ElectronicSignedDataConstruct {
                Issuer = esdc.Issuer,
                Subject = "prod-other",
                IssuedAt = esdc.IssuedAt,
                KeyId = esdc.KeyId,
                Format = esdc.Format,
                SignatureAlgorithm = esdc.SignatureAlgorithm,
                VerifiableCredentialJwt = esdc.VerifiableCredentialJwt,
                PublicKey = esdc.PublicKey,
                Certificate = esdc.Certificate
            };

            Assert.False(service.Verify(reSubjected));
        }
            [Fact]
            public void SharedSigningKey_KeepsCredentialsVerifiableAcrossInstances()
            {
                // The scenario a persisted key exists to protect: a restart (or a second replica) must be
                // able to verify ESDCs issued earlier. Both instances get the same resolved key, standing
                // in for one loaded from the database or a secret store.
                using RSA key = RSA.Create(2048);
                string privateKeyPem = key.ExportPkcs8PrivateKeyPem();

                using var before = new RsaEsdcService(null, privateKeyPem);
                ElectronicSignedDataConstruct esdc = before.Issue(SampleDpp());

                using var afterRestart = new RsaEsdcService(null, privateKeyPem);
                Assert.True(afterRestart.Verify(esdc));
            }

            [Fact]
            public void ResolvedKeyConstructor_RefusesToRunWithoutAKey()
            {
                // Silently falling back to an ephemeral key here is exactly the defect being fixed, so the
                // production constructor fails loudly instead.
                Assert.Throws<InvalidOperationException>(() => new RsaEsdcService(null, null));
            }

            [Fact]
            public void MismatchedCertificate_FailsFast()
            {
                // The certificate is published as x5c and its thumbprint as kid, so a certificate for a
                // different key would tell verifiers that a key which cannot verify the signature
                // produced the credential - silently defeating certificate-backed verification.
                using RSA signingKey = RSA.Create(2048);
                using RSA otherKey = RSA.Create(2048);

                var request = new CertificateRequest("CN=other", otherKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                using X509Certificate2 unrelatedCert = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(1));

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> {
                        ["Dpp:Esdc:PrivateKeyPem"] = signingKey.ExportPkcs8PrivateKeyPem(),
                        ["Dpp:Esdc:CertificatePem"] = ExportCertificatePem(unrelatedCert)
                    })
                    .Build();

                InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
                    () => new RsaEsdcService(configuration));

                Assert.Contains("does not match the ESDC signing key", ex.Message, StringComparison.Ordinal);
            }

            [Fact]
            public void MatchingCertificate_IsAccepted()
            {
                // The supported configuration: a certificate issued for the signing key itself.
                using RSA signingKey = RSA.Create(2048);

                var request = new CertificateRequest("CN=issuer", signingKey, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
                using X509Certificate2 matchingCert = request.CreateSelfSigned(
                    DateTimeOffset.UtcNow.AddDays(-1),
                    DateTimeOffset.UtcNow.AddDays(1));

                IConfiguration configuration = new ConfigurationBuilder()
                    .AddInMemoryCollection(new Dictionary<string, string> {
                        ["Dpp:Esdc:PrivateKeyPem"] = signingKey.ExportPkcs8PrivateKeyPem(),
                        ["Dpp:Esdc:CertificatePem"] = ExportCertificatePem(matchingCert)
                    })
                    .Build();

                using var service = new RsaEsdcService(configuration);
                ElectronicSignedDataConstruct esdc = service.Issue(SampleDpp());

                // The advertised certificate must actually verify what it claims to have signed.
                Assert.NotNull(esdc.Certificate);
                Assert.True(service.Verify(esdc));
            }

            [Fact]
            public void Verify_RequiresSignedValidFrom_WhenEnvelopeSuppliesIssuedAt()
            {
                // An envelope timestamp must be backed by a signed value. If an absent or wrongly
                // typed validFrom simply skipped the comparison, the envelope could carry any
                // IssuedAt it liked and nothing signed would contradict it.
                using RSA key = RSA.Create(2048);
                string privateKeyPem = key.ExportPkcs8PrivateKeyPem();

                using var service = new RsaEsdcService(null, privateKeyPem);
                ElectronicSignedDataConstruct esdc = service.Issue(SampleDpp());

                // Re-sign a credential with validFrom removed, so the signature stays genuine and
                // only the metadata binding is under test.
                string[] parts = esdc.VerifiableCredentialJwt.Split('.');
                using JsonDocument payload = JsonDocument.Parse(Base64Url.DecodeFromChars(parts[1]));

                var stripped = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
                foreach (JsonProperty property in payload.RootElement.EnumerateObject())
                {
                    if (!string.Equals(property.Name, "validFrom", StringComparison.Ordinal))
                    {
                        stripped[property.Name] = property.Value;
                    }
                }

                byte[] strippedPayload = JsonSerializer.SerializeToUtf8Bytes(stripped);
                string encodedPayload = Base64Url.EncodeToString(strippedPayload);
                string signingInput = parts[0] + "." + encodedPayload;
                byte[] signature = key.SignData(Encoding.UTF8.GetBytes(signingInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                var tampered = new ElectronicSignedDataConstruct {
                    Issuer = esdc.Issuer,
                    Subject = esdc.Subject,
                    IssuedAt = esdc.IssuedAt,
                    KeyId = esdc.KeyId,
                    Format = esdc.Format,
                    SignatureAlgorithm = esdc.SignatureAlgorithm,
                    VerifiableCredentialJwt = signingInput + "." + Base64Url.EncodeToString(signature),
                    PublicKey = esdc.PublicKey,
                    Certificate = esdc.Certificate
                };

                Assert.False(service.Verify(tampered));
            }

            private static string ExportCertificatePem(X509Certificate2 certificate) =>
                new string(PemEncoding.Write("CERTIFICATE", certificate.RawData));
        }
    }
