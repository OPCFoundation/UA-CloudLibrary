using System;
using System.Buffers.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
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
        }
    }
