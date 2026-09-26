# Digital Product Passport (DPP)

The UA Cloud Library hosts a Digital Product Passport (DPP) Lifecycle API that exposes selected OPC UA information models as DPPs.

This document is for people **using** or **operating** a UA Cloud Library deployment: reading and updating passports over the API, controlling who may see which data, and configuring signing and auditing. It does not describe the server's internal design.

- [Standards](#standards)
- [Conceptual model](#conceptual-model)
- [Serialization](#serialization)
- [Response envelope](#response-envelope)
- [Endpoints](#endpoints)
- [Addressing elements](#addressing-elements)
- [Version history](#version-history)
- [Controlled access](#controlled-access)
- [Audit log](#audit-log)
- [Electronic Signed Data Constructs (ESDC)](#electronic-signed-data-constructs-esdc)
- [Configuration reference](#configuration-reference)
- [Standards conformance](#standards-conformance)
- [Error responses](#error-responses)

## Standards

The API is aligned with five European Norms:

* **EN 18221** &mdash; *Data storage, archiving, and data persistence* &mdash; storage, archiving and version retrieval.
* **EN 18222** &mdash; *Application Programming Interfaces (APIs) for the product passport lifecycle management and searchability* &mdash; the REST surface.
* **EN 18223** &mdash; *System interoperability* &mdash; the semantic data model and its JSON serialization.
* **EN 18239** &mdash; *Access rights, IT security and business confidentiality* &mdash; the public/controlled access split and element-level role-based access control.
* **EN 18246** &mdash; *Data authentication, reliability and integrity* &mdash; the tamper-evident audit log, Electronic Signed Data Constructs and unauthenticated public read.

## Conceptual model

A DPP is constructed on demand from an OPC UA nodeset that has been uploaded to the Cloud Library. Its header properties and its tree of data elements are read from the nodeset's live address space. The DPP identifier (`dppId`) is the nodeset identifier the Cloud Library issues on upload, so any nodeset you upload is addressable as a passport without a separate registration step.

The DPP header follows EN 18223 Clause 4.1.2.1 Table 1:

| Property | Cardinality | Notes |
|---|---|---|
| `digitalProductPassportId` | [1] | Globally unique, opaque string. This server emits the Cloud Library nodeset identifier (e.g. `"3851629631"`), so callers can round-trip the value back through the `v1/dpps/{dppId}` endpoints. |
| `uniqueProductIdentifier` | [1] | Product identifier per EN 18219. |
| `granularity` | [1] | Enumeration: `model`, `batch`, `item` (lowercase on the wire). |
| `dppSchemaVersion` | [1] | Reference standard the DPP schema follows. |
| `dppStatus` | [1] | e.g. `active`, `inactive`, `archived`, `invalid`. |
| `lastUpdate` | [1] | UTC timestamp per ISO 8601-1. |
| `economicOperatorId` | [1] | Operator identifier per EN 18219. |
| `facilityId` | [0..1] | Facility identifier per EN 18219. |
| `contentSpecificationIds` | [0..*] | References to horizontal or product-type content specifications. |
| `elements` | [0..*] | Tree of data elements. |

Each data element declares its kind in an `objectType` property:

| `objectType` | Purpose |
|---|---|
| `DataElementCollection` | A named container of child elements (mixed types allowed). |
| `SingleValuedDataElement` | A leaf carrying a single value (any JSON primitive, object or array). |
| `MultiValuedDataElement` | A leaf carrying a homogenous, non-empty list of nested elements, serialized under `value`. |
| `RelatedResource` | A reference to an external resource (document, certificate) with `contentType`, `url`, optional `language` and `resourceTitle`. |
| `MultiLanguageDataElement` | A language-dependent value with one or more `{ value, language }` entries under `value`. |

> **`elementId` is a server-generated GUID.** Each element's `elementId` is a stable GUID derived from the underlying OPC UA node's identity. It is globally unique even when names collide across namespaces, and it is the same on every read &mdash; so element addressing and the per-DPP access mapping stay stable. The illustrative payloads below use readable ids (e.g. `maxPressure`) for legibility, matching the EN 18223 Annex A examples; **a live response carries GUID `elementId`s**. An element's human meaning is conveyed by its `dictionaryReference` (e.g. IEC CDD), not by its id.

## Serialization

EN 18223 defines two equivalent JSON serializations: a **compressed** form, where each element uses its `elementId` as the JSON object key and its type information is looked up from an external data dictionary, and an **expanded** form, where every element is a self-describing JSON object.

**This server emits and accepts the expanded form.** That keeps payloads self-describing, so you do not need to resolve dictionary references in order to interpret a value. The shape matches the EN 18223 Annex A examples:

| EN 18223 subclass | JSON shape |
|---|---|
| `DigitalProductPassport` | Top-level object with the header properties above and an `elements` array. |
| `DataElementCollection` | `{ "objectType": "DataElementCollection", "elementId": ..., "elements": [ ... ] }` |
| `SingleValuedDataElement` | `{ "objectType": "SingleValuedDataElement", "elementId": ..., "valueDataType": ..., "value": <any JSON type> }` |
| `MultiValuedDataElement` | `{ "objectType": "MultiValuedDataElement", "elementId": ..., "valueDataType": ..., "value": [ ...same-type elements... ] }` |
| `RelatedResource` | `{ "objectType": "RelatedResource", "elementId": ..., "contentType": ..., "url": ..., "language": ..., "resourceTitle": ... }` |
| `MultiLanguageDataElement` | `{ "objectType": "MultiLanguageDataElement", "elementId": ..., "value": [ { "value": ..., "language": ... } ] }` |

## Response envelope

Every endpoint returns the same envelope:

```json
{
  "status": "Success",
  "payload": { },
  "result": { "messages": [ { "code": "Error", "message": "..." } ] },
  "pagination": { "nextCursor": "100" },
  "esdc": { }
}
```

`payload` carries the requested data. `result` is present only when there are messages to report. `pagination` appears only on paged responses, and `esdc` only where a signed construct is produced &mdash; see [ESDC](#electronic-signed-data-constructs-esdc).

## Endpoints

All routes are versioned under `v1/`.

**Read routes are reachable anonymously**, so public DPP data can be read without logging in (EN 18246 Clause 5.1). Authenticated callers additionally see any controlled elements their roles permit. **Write routes require authentication** and ownership of the underlying nodeset.

| Method | Route | Description |
|---|---|---|
| `GET` | `v1/dpps/{dppId}` | Returns the full DPP. |
| `GET` | `v1/dppsByProductId/{productId}` | Returns the DPP for a unique product identifier. |
| `POST` | `v1/dppsByProductIds` | Resolves a batch of product identifiers to DPP ids. Accepts up to 100 ids per request; supports `limit` and `cursor` for paging. |
| `GET` | `v1/dpps/{dppId}/elements/{elementIdPath}` | Returns a single data element. |
| `GET` | `v1/dpps/{dppId}/versions/{date}` | Returns the DPP as it stood at an ISO 8601 timestamp. See [version history](#version-history). |
| `PATCH` | `v1/dpps/{dppId}` | Partially updates the DPP header. |
| `PATCH` | `v1/dpps/{dppId}/elements/{elementIdPath}` | Updates a single data element's value. |

## Addressing elements

`elementIdPath` is a JSONPath expression rooted at the DPP's `elements` collection. The supported subset is:

* Optional `$` root, which may be omitted.
* Dot child selector: `.name`
* Bracket name selector, single or double quoted: `['name']`, `["name"]`
* Bracket index selector: `[0]`, `[3]`

Filter expressions, wildcards (`*`), slice selectors and the descendant operator (`..`) are rejected as a bad request. Examples:

```
manufacturer
materials.copperContent
$['battery']['cells']['voltage']
```

> **Address elements by name, not by index.** Access rights are keyed by the element's `elementId` chain, and an index names a position rather than an element &mdash; so a path containing an index selector cannot be matched against the access mapping. Such a request is therefore refused rather than served, and returns `404` exactly as a controlled element does. Index selectors remain valid for *resolving* an element, so they still work anywhere authorization is not involved, but a read or write of a specific element should address it by name.

## Version history

Every change to a DPP is archived, so any past version remains retrievable for the lifetime of the passport (EN 18221 Clause 4.2). `GET v1/dpps/{dppId}/versions/{date}` returns the version that was active at the timestamp you supply: if the timestamp is at or after the passport's own `lastUpdate`, you get the current version; otherwise you get the most recent archived snapshot at or before that moment. A `404` means no version of that DPP existed yet at the requested time.

Archiving is part of the update contract: if a snapshot cannot be stored, the update itself fails rather than silently losing a version.

> **Historical reads apply the access policy of their own version.** A snapshot is filtered against the controlled-element mapping that was in force when it was captured, not today's. Removing an element from the mapping therefore does not retroactively publish it in older snapshots.
>
> Snapshots archived by older versions of the server carry no recorded policy. Because that policy cannot be reconstructed after the fact, those versions return the passport's public header with **no** data elements rather than guessing. Re-archiving restores element-level history for them.

## Controlled access

Read access is **public by default**. To restrict an element, list it in a reserved `controlledElements` object in the values file you upload alongside the nodeset. The mapping is per DPP, and it maps an element **path** to the role or roles permitted to read that element and everything beneath it.

The values file is otherwise the flat `{ nodeId: value }` map already used to seed variable values:

```json
{
  "nsu=http://example/dpp;i=6001": "750.0",
  "nsu=http://example/dpp;i=6002": "0.95",
  "controlledElements": {
    "7d3a2b18-2b7c-5e41-9f0a-1c2d3e4f5a6b.2c1f9e8d-4a5b-5c6d-8e7f-0a1b2c3d4e5f": [ "Recycler", "Repairer" ],
    "9b8c7d6e-1a2b-5c3d-8e4f-5a6b7c8d9e0f": "Recycler"
  }
}
```

Access is keyed by element **path** &mdash; the dotted chain of `elementId` values, the same address the API uses &mdash; deliberately *not* by `dictionaryReference`, which EN 18223 reserves for semantic meaning and would be unsuitable as an access key. Since each `elementId` is a stable GUID, read the DPP once to obtain the paths you want to control, then reference them in the mapping. Controlling a container path controls its whole subtree.

> **A mapping that cannot be read unambiguously denies access rather than granting it.** The reserved property is matched case-insensitively, but JSON allows several spellings to coexist. A file carrying both `controlledElements` and `ControlledElements` has no single authoritative policy, so it is rejected as deny-all rather than letting an empty decoy shadow a real mapping. The same applies to an unparseable file and to `"controlledElements": null`: a policy that cannot be understood is never treated as an absent one.

> **An unavailable policy source is not an absent policy.** If the storage holding the mapping cannot be reached, or the passport has no stored values row at all, elements are withheld rather than published. Because reads are anonymous, treating either case as "this passport controls nothing" would expose every controlled element. This matters specifically because a DPP is built from the live OPC UA address space: it can still be served while its stored row is missing, so the missing row must deny rather than default to public. For the same reason, exporting or saving a nodeset is refused when its stored values cannot be loaded: writing the browse output alone would drop the `controlledElements` map, and re-uploading that file would publish everything it protected.

> **Filtering covers nested elements.** Controlled elements are withheld wherever they appear in the tree, including inside multi-valued elements, and are excluded from the signed ESDC as well as from the response body.

## Audit log

Reads and changes to DPP data are recorded in an append-only, hash-chained audit log (EN 18246 Clause 4.7), making a change non-repudiable and retrospective tampering detectable.

**If an operation cannot be recorded, it is refused.** A read that cannot be logged returns `503` without disclosing data; a change that cannot be logged is not applied.

> **What the audit log does and does not prove.** Configure `Dpp__Audit__HmacKey` (base64, at least 32 bytes, from a managed secret store) to make the chain tamper-evident against someone who can write to the database. Without a key the log is integrity-checked but *not* tamper-proof: anyone able to modify the audit tables can recompute the digests and pass verification.
>
> Even with a key, this does not defend against an attacker holding **both** the key and database write access, and it does not prove *when* an entry was written. Deployments needing evidence against a compromised database operator should additionally anchor the log in an external append-only store (a WORM bucket, a transparency log, or periodic off-host export). That is not provided here.

> **Verification has to be invoked to be worth anything.** Hash-chaining makes tampering detectable, but nothing detects it unless the chain is actually re-walked. `GET /health/dpp-audit` re-verifies the log and reports `Unhealthy` whenever it cannot be vouched for, so a problem surfaces through whatever already monitors the service. It requires administrator rights and reads every audit row, so point a readiness or monitoring schedule at it rather than a liveness probe.
>
> The status alone does not tell you which problem you have &mdash; read the description, which distinguishes three cases:
>
> * **Altered or truncated.** The chain does not match itself or the checkpoint. This is a tampering signal.
> * **Not authenticated.** The checkpoint predates the configured audit key and the migration has not been enabled. Just as likely an un-migrated deployment as an attack, so it is reported as a migration rather than an intrusion &mdash; see [rotating the audit key](#rotating-the-audit-key).
> * **Could not be verified.** An entry was signed with a key that is no longer configured, so its integrity is unknown rather than disproven. Restore the retired key to resolve it.
>
> The separation is deliberate: reporting an ordinary key rotation or a pending migration as tampering is a false alarm on a security control, and an alert that cries wolf gets muted &mdash; which costs you the real signal.

> **A failed audit append is reported differently depending on whether anything changed.** If nothing was applied, you get a retryable `503` and repeating the request is correct. If the change already committed and only its completion record failed, you get a `500` stating the change was applied and must **not** be retried &mdash; retrying would apply it twice.

> **A partly-applied upload is recorded as such.** An upload writes the nodeset, then its metadata, then indexes it, so it can fail with the nodeset already stored. That outcome is recorded as `PartialFailure` rather than `Failed`, and treated as a committed change: the response still reports the error, but the audit trail shows storage was touched, and a failure of that audit write is not presented as a safe retry. Re-uploading after a `PartialFailure` is the normal recovery &mdash; use the overwrite flag, since the nodeset may already be present.
>
> The same applies to **approving** a nodeset, which copies it and can therefore create a passport: approvals write the same intent and outcome records as a direct upload, and an approval that cannot be audited is refused rather than completed silently.

> **Durability limitation.** A change touches several stores that share no transaction, so each change writes an intent record before it and a closing record after it. Every known outcome writes a closing record, which means an unmatched intent record specifically indicates the outcome is *unknown* &mdash; the process died mid-operation, or the closing write itself failed &mdash; rather than that the request was merely refused. This makes the gap **detectable**, not impossible; true atomicity would require a transactional outbox, which is not implemented.

### Rotating the audit key

Each entry records a fingerprint of the key that signed it, and verification uses *that* key rather than whichever key is configured at verification time. Without this, enabling or rotating a key would make every earlier entry fail verification and report tampering that never occurred &mdash; a false alarm on a tamper-evidence control is worse than no alarm, because it teaches operators to ignore it.

To rotate:

1. Move the current value of `Dpp__Audit__HmacKey` into the next free `Dpp__Audit__RetiredHmacKeys__N` slot.
2. Set `Dpp__Audit__HmacKey` to the new key.
3. Restart. New entries are signed with the new key; existing entries continue to verify against the retired one.

Entries written before any key was configured keep verifying as unkeyed, so enabling a key for the first time needs no backfill. It does need one deliberate step: set `Dpp__Audit__AllowUnkeyedCheckpointMigration` to `true` for the first keyed run, then remove it.

> **Retired keys must be kept for as long as the entries they signed are retained.** Dropping one does not cause a false tampering report &mdash; those entries are reported as *unverifiable*, naming the missing fingerprint &mdash; but it is not recoverable: nothing can re-establish their integrity afterwards. Treat retired audit keys as part of the audit record's retention policy, not as expired secrets to clean up.

> **An unkeyed checkpoint is refused once keyed auditing is enabled**, unless the migration setting above is explicitly on. This cannot be inferred safely from the log itself: an attacker who can rewrite history could also clear the key fingerprints and make a forged chain look like untouched pre-key history. Accepting an unkeyed checkpoint is therefore a deliberate operator decision, and the setting should be left off in steady state.

> **A missing checkpoint blocks further changes when entries already exist.** Deleting the checkpoint along with recent entries would otherwise be laundered by the next legitimate write. **This is deliberately disruptive** &mdash; DPP changes stop until an operator restores the checkpoint from backup or re-initializes the audit log on purpose, because continuing would silently certify a log that may have been truncated.

## Electronic Signed Data Constructs (ESDC)

Full-DPP reads return an ESDC alongside the payload: a W3C Verifiable Credential secured as a JSON Web Signature (`application/vc+jwt`, RS256), binding the passport's data to its economic operator and product. Verification needs only the public key, so it is free and unrestricted (EN 18246 Clause 4.5). The compact key id is also returned in an `X-DPP-ESDC-KeyId` response header so a verifier can select a trust anchor without parsing the body.

An ESDC is omitted when the server holds no signing material for that passport's economic operator &mdash; the read still succeeds.

### Signing key management

The signing key is the sole basis on which a verifier decides an ESDC is authentic, so it **must be stable**. A key that changes between restarts, or differs between instances behind a load balancer, silently invalidates every ESDC issued under the previous key.

The key is resolved in this order:

| Order | Source | When to use |
| --- | --- | --- |
| 1 | `Dpp__Esdc__PrivateKeyPem` | **Required in production.** Supplied from a managed secret store; never stored in application data. |
| 2 | Previously generated key in the database | Reused automatically once generated, so it survives restarts and is shared by all instances. |
| 3 | Newly generated RSA-2048 | Only when neither of the above exists, **and** only in Development or with `Dpp__Esdc__AllowGeneratedSigningKey=true`. |

> **The server fails to start** outside Development if no key is configured and `Dpp__Esdc__AllowGeneratedSigningKey` is not `true`. Signing production ESDCs with a key held in application data is a deliberate downgrade, so it has to be a decision made on purpose. Starting cleanly and only revealing the problem at the first read would be worse: by then the weak key is already in use and in your backups.

> **On the generated fallback key.** If no key is configured, the server generates one and stores it in the database. That keeps ESDCs verifiable across restarts and replicas, which an in-memory key cannot do &mdash; but the private key is then present in every database backup and readable by anything with database access. It is intended for development and evaluation only.

### Issuer binding

An ESDC asserts *who* issued a passport, but the `issuer` claim is copied from the passport's own `economicOperatorId` &mdash; content this server merely hosts, not a statement about who holds the signing key. Nothing in the document itself stops a passport naming one operator from being signed by a server belonging to another.

Set `Dpp__Esdc__EconomicOperatorId` to the operator this deployment actually represents. Two things then hold:

* **Issuance is refused** for any DPP naming a different operator, rather than producing a credential asserting an identity this key does not hold.
* **The server's own key is bound** to that identifier, so a credential naming a different operator cannot be verified against it either.

> **This setting is required wherever the signing key is.** An optional guard against impersonation is no guard at all for the deployment that forgets it, so a server resolving a stable signing key refuses to start without an operator id. **Existing deployments must set it as part of the upgrade**, otherwise the server will not start.

### Generating a key

```bash
# Private key (PKCS#8 PEM) - keep secret
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out esdc-signing-key.pem

# Public key - safe to publish; this is what verifiers configure as a trust anchor
openssl rsa -pubout -in esdc-signing-key.pem -out esdc-public-key.pem
```

Optionally issue a certificate over the same key and supply it as `Dpp__Esdc__CertificatePem`; it is embedded in the signature header so verifiers can chain the key to an accredited economic operator.

### Storing it securely

Provide the PEM through configuration &mdash; never commit it to source control or bake it into a container image:

- **Azure Key Vault** (recommended for the hosted deployment): store as secret `Dpp--Esdc--PrivateKeyPem` and reference it from App Service / Container Apps.
- **Environment variable**: `Dpp__Esdc__PrivateKeyPem`.
- **Kubernetes**: mount a `Secret` and project it into that environment variable.
- **Local development**: `dotnet user-secrets set "Dpp:Esdc:PrivateKeyPem" "$(cat esdc-signing-key.pem)"` &mdash; keeps it out of `appsettings.json`.

Restrict read access to the application identity, and rely on the secret store's own audit log to record access.

### Rotation

Rotating the key changes the key id and invalidates ESDCs signed with the old key, so treat it as a coordinated change:

1. Generate a new key pair and distribute the **new public key** to verifiers as an additional trust anchor.
2. Keep the old public key trusted until previously issued ESDCs are no longer relied upon &mdash; both keys can be trusted simultaneously.
3. Update `Dpp__Esdc__PrivateKeyPem` and restart. A configured key always takes precedence, so this also cleanly supersedes a generated fallback key.
4. Once the old key is retired, remove it from the verifiers' trust anchors.

### Trusting other operators

To verify ESDCs issued by *other* economic operators, bind each trusted key to the issuer it may sign for:

```json
"Dpp": {
  "Esdc": {
    "TrustedIssuers": [
      { "Issuer": "EO-12345", "PublicKeyPem": "-----BEGIN PUBLIC KEY-----\n...", "KeyId": "optional-kid" }
    ]
  }
}
```

The binding is the point: a bare list of trusted keys establishes only that *someone* trusted signed the credential, so any trusted peer could issue one naming a different operator and it would still verify. Verification selects the anchor from the **signed** issuer before checking the signature. An anchor configured without an `Issuer`, or with a malformed key, fails startup rather than silently narrowing trust.

The older flat `Dpp__Esdc__TrustedPublicKeysPem` list is still read so existing deployments keep working, but those keys carry no issuer binding and are refused whenever a bound anchor already claims the signed issuer. Prefer `TrustedIssuers` for new configuration.

## Configuration reference

A double underscore (`__`) in an environment variable key creates a nested configuration section.

### Signing

| Setting | Description |
|---|---|
| `Dpp__Esdc__EconomicOperatorId` | The economic operator this server may sign for. **Required in production** &mdash; the server refuses to start without it. |
| `Dpp__Esdc__PrivateKeyPem` | PKCS#8 PEM private key used to sign ESDCs. **Required in production** unless `Dpp__Esdc__AllowGeneratedSigningKey` is `true`. |
| `Dpp__Esdc__AllowGeneratedSigningKey` | Permit a server-generated key stored in the database outside Development. (default: `false`) |
| `Dpp__Esdc__CertificatePem` | Optional issuer certificate PEM, embedded in the signature header. |
| `Dpp__Esdc__TrustedIssuers__0__Issuer`, `__0__PublicKeyPem`, `__0__KeyId` | A trusted peer bound to the issuer it may sign for. `KeyId` is optional and pins the signature's key id. |
| `Dpp__Esdc__TrustedPublicKeysPem__0`, `__1`, ... | Legacy, unbound public keys of peer operators. Prefer the issuer-bound form above. |

### Auditing

| Setting | Description |
|---|---|
| `Dpp__Audit__HmacKey` | Base64 key (at least 32 bytes) authenticating the audit chain. Without it the chain is integrity-checked but not tamper-evident against database modification. |
| `Dpp__Audit__RetiredHmacKeys__0`, `__1`, ... | Previously used audit keys, retained so entries signed with them stay verifiable after a rotation. |
| `Dpp__Audit__AllowUnkeyedCheckpointMigration` | Set to `true` only while enabling an audit key over an existing unkeyed log. Leave unset otherwise. |

### Rate limiting and proxies

| Setting | Description |
|---|---|
| `Dpp__RateLimit__PermitPerMinute` | Requests permitted per minute per client IP on the DPP endpoints. Must be greater than zero; the server refuses to start otherwise. (default: `100`) |
| `Dpp__ForwardedHeaders__KnownProxies__0`, `__1`, ... | IP addresses of trusted reverse proxies whose `X-Forwarded-For`/`X-Forwarded-Proto` headers should be honoured. |
| `Dpp__ForwardedHeaders__KnownNetworks__0`, `__1`, ... | Trusted proxy networks in CIDR form (e.g. `10.0.0.0/8`). |
| `Dpp__ForwardedHeaders__TrustAllProxies` | Set to `true` **only** when the server is network-isolated behind a proxy and cannot be reached directly. (default: `false`) |

> **On forwarded headers.** By default only loopback proxies are trusted. If the server runs behind a reverse proxy and none of these settings are configured, the per-IP rate limit will partition on the proxy's address &mdash; collapsing every caller into one bucket &mdash; and redirects may be built from the wrong scheme. Configure the proxy's address or network rather than `TrustAllProxies` wherever possible: trusting all proxies lets any caller who can reach the server directly spoof their client IP to evade rate limiting.

## Standards conformance

**Access rights and business confidentiality (EN 18239 Clause 5.2).** Element-level role-based access control, public-by-default reads, and operator identification on every audited operation.

**Access-rights management and revocation (EN 18239 Clause 5.2(16)/(17)/(19), Clause 6.3).** Roles and their assignments are managed through the access API. Revoking a role also invalidates the affected user's existing sessions, so a revoked role stops granting access immediately rather than when the session cookie happens to expire. The last administrator cannot be demoted, so a deployment cannot be locked out of its own administration.

**Abuse prevention and rate limiting (EN 18239 Clause 5.2(11)/(15)).** The DPP endpoints are rate-limited per client IP, so anonymous public reads cannot be used to exhaust the service.

**Data authentication and integrity (EN 18246 Clause 4.5, 4.7, 5.1).** ESDCs over full-DPP reads, a tamper-evident audit log, and unauthenticated public read of non-controlled data.

**Storage and archiving (EN 18221 Clause 4.1, 4.2).** Durable storage and point-in-time retrieval of every past version.

**Interoperability (EN 18223).** The EN 18223 data model, serialized in the expanded Annex A form.

## Error responses

| Status | Meaning |
|---|---|
| `400` | Malformed request: an unsupported `elementIdPath`, an invalid timestamp, an empty or oversized batch. |
| `401` / `403` | Authentication missing, or the caller does not own the underlying nodeset. |
| `404` | No such DPP, element, or no version at the requested time. |
| `429` | Rate limit exceeded. |
| `500` | The change was applied but could not be fully recorded. **Do not retry**; verify current state first. |
| `503` | The request was refused because it could not be audited. Nothing was applied; retrying is safe. |
