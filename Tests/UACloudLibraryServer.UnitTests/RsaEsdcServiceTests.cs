using System;
using System.Buffers.Text;
using System.Text;
using System.Text.Json;

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
        public void Credential_IsIndependentlyVerifiableByAnotherInstance()
        {
            using var issuer = new RsaEsdcService(null);
            ElectronicSignedDataConstruct esdc = issuer.Issue(SampleDpp());

            // A different service instance (with its own ephemeral key) must still verify the VC, because
            // verification uses the public key embedded in the credential/envelope.
            using var independentVerifier = new RsaEsdcService(null);
            Assert.True(independentVerifier.Verify(esdc));
        }
    }
}
