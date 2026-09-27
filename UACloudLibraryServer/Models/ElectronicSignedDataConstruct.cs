using System;
using System.Text.Json.Serialization;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// An Electronic Signed Data Construct (EN 18246:2026, def 3.1 and Annex A) implemented as a W3C
    /// Verifiable Credential secured with an enveloped JWS (<c>application/vc+jwt</c>, Annex B.5). The
    /// authoritative, portable artifact is <see cref="VerifiableCredentialJwt"/> — a standard compact
    /// JWS that any VC-JWT verifier can validate. The remaining properties mirror its header for
    /// convenience and carry the issuer's verification material (<see cref="PublicKey"/> and, when
    /// certificate-backed, <see cref="Certificate"/>) so the credential is independently verifiable,
    /// free of charge and without contacting the issuer (Annex A.3, §4.7). Trust in the issuer's
    /// certificate itself derives from the applicable trusted list / governance framework, which is a
    /// deployment concern.
    /// </summary>
    public sealed class ElectronicSignedDataConstruct
    {
        // The issuing economic operator, per EN 18219 unique operator identifier.
        [JsonPropertyName("issuer")]
        public string Issuer { get; init; }

        // The product the credential is about (unique product identifier).
        [JsonPropertyName("subject")]
        public string Subject { get; init; }

        // Header: issuance timestamp (UTC); mirrors the credential's validFrom.
        [JsonPropertyName("issuedAt")]
        public DateTimeOffset IssuedAt { get; init; }

        // Header: identifies the signing key/certificate (certificate thumbprint, or a hash of the
        // public key when no certificate is used) so verifiers can select the correct trust anchor.
        [JsonPropertyName("keyId")]
        public string KeyId { get; init; }

        // The media type of the secured credential.
        [JsonPropertyName("format")]
        public string Format { get; init; }

        // The JOSE signature algorithm (e.g. RS256).
        [JsonPropertyName("signatureAlgorithm")]
        public string SignatureAlgorithm { get; init; }

        // The W3C Verifiable Credential secured as a compact JWS (application/vc+jwt). This is the
        // authoritative, self-contained, independently verifiable artifact.
        [JsonPropertyName("verifiableCredential")]
        public string VerifiableCredentialJwt { get; init; }

        // Issuer's public key (SubjectPublicKeyInfo PEM). Lets a verifier check the JWS without
        // contacting the issuer when no certificate chain is embedded in the JWS header.
        [JsonPropertyName("publicKey")]
        public string PublicKey { get; init; }

        // Issuer's X.509 certificate (base64 DER), present when the signer is certificate-backed.
        // Lets verifiers check the issuer's authority/accreditation against a trusted list.
        [JsonPropertyName("certificate")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Certificate { get; init; }
    }
}
