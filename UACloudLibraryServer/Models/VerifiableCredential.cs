using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Opc.Ua.Cloud.Library.Models
{
    /// <summary>
    /// A W3C Verifiable Credential (VC Data Model 2.0) carrying a DPP as its credential subject. This is
    /// the unsecured credential document; it is secured as an enveloped JWS (<c>application/vc+jwt</c>)
    /// per the W3C "Securing Verifiable Credentials using JOSE and COSE" recommendation, which is one of
    /// the ESDC formats recognized by EN 18246 (Annex B.5).
    /// </summary>
    public sealed class VerifiableCredential
    {
        [JsonPropertyName("@context")]
        public List<string> Context { get; init; } = new() { "https://www.w3.org/ns/credentials/v2" };

        // Optional credential identifier (a URI); a urn:uuid is used by default.
        [JsonPropertyName("id")]
        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string Id { get; init; }

        [JsonPropertyName("type")]
        public List<string> Type { get; init; } = new() { "VerifiableCredential" };

        // The issuer identifier (EN 18219 unique operator identifier; should be a URI).
        [JsonPropertyName("issuer")]
        public string Issuer { get; init; }

        // VC 2.0 validity start (ISO 8601 UTC).
        [JsonPropertyName("validFrom")]
        public DateTimeOffset ValidFrom { get; init; }

        [JsonPropertyName("credentialSubject")]
        public DppCredentialSubject CredentialSubject { get; init; }
    }

    /// <summary>
    /// The <c>credentialSubject</c> of a DPP Verifiable Credential: the product (identified by its unique
    /// product identifier) and the digital product passport asserted about it.
    /// </summary>
    public sealed class DppCredentialSubject
    {
        // The subject identifier: the unique product identifier (a URI per EN 18219).
        [JsonPropertyName("id")]
        public string Id { get; init; }

        [JsonPropertyName("digitalProductPassport")]
        public DigitalProductPassport DigitalProductPassport { get; init; }
    }
}
