# UA Cloud Library

The reference implementation of the UA Cloud Library. The UA Cloud Library enables the storage and querying of OPC UA Information Models from anywhere in the world.

## Build Status

[![Docker Image CI](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml)

[![.NET](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml)

## Table of Contents

- [Features](#features)
- [Architecture](#architecture)
- [Using the UA Cloud Library from a Client Application](#using-the-ua-cloud-library-from-a-client-application)
  - [Client Library Installation](#client-library-installation)
  - [Authentication Options](#authentication-options)
- [Development Setup](#development-setup)
- [Authentication and Authorization](#authentication-and-authorization)
- [Digital Product Passport (DPP)](#digital-product-passport-dpp)
  - [Conceptual model](#conceptual-model)
  - [Serialization (EN 18223 Clause 5 / Annex A)](#serialization-en-18223-clause-5--annex-a)
  - [Response envelope](#response-envelope)
  - [Endpoints](#endpoints)
  - [`elementIdPath` &mdash; JSONPath addressing](#elementidpath--jsonpath-addressing)
  - [Write semantics, archival and persistence](#write-semantics-archival-and-persistence)
  - [Durable version archive](#durable-version-archive)
  - [Access control, audit and signing (EN 18239 / EN 18246)](#access-control-audit-and-signing-en-18239--en-18246)
  - [ESDC signing key management](#esdc-signing-key-management)
  - [Standards conformance (EN 18239 / EN 18246)](#standards-conformance-en-18239--en-18246)
  - [Error responses](#error-responses)
- [Database Configuration](#database-configuration)
- [Cloud Hosting Setup](#cloud-hosting-setup)
  - [Migrating from version 1.0 to version 1.1](#migrating-from-version-10-to-version-11)
  - [Required Settings - PostgreSQL](#required-settings---postgresql)
  - [Setting Password for Admin Account](#setting-password-for-admin-account)
  - [Optional Settings](#optional-settings)
  - [Optional Settings - Captcha](#optional-settings---captcha)
  - [Optional Settings - Data Protection](#optional-settings---data-protection)
- [Deployment](#deployment)
- [Security &ndash; STRIDE Threat Analysis (UA-CloudLibrary server)](#security--stride-threat-analysis-ua-cloudlibrary-server)
  - [API Key Security Features](#api-key-security-features)
  - [Residual recommendations for operators](#residual-recommendations-for-operators)

## Features

* REST interfaces
* Swagger UI
* User management UI
* OPC UA Information Model upload and download
* OPC UA Information Model browse and search
* Simple OPC UA Information Model authoring UI
* Cross-platform: Runs on any edge or cloud that can host a container and a PostgreSQL instance

## Architecture

The UA Cloud Library is implemented as a set of Docker containers. The main container hosts the REST API and the user management website. A PostgreSQL database is used to store the information models and user data:

![Architecture](https://github.com/OPCFoundation/UA-CloudLibrary/blob/main/Docs/architecture.png)

## Using the UA Cloud Library from a Client Application

If you want to access your own instance or the globally hosted instance from the OPC Foundation at https://uacloudlibrary.opcfoundation.org from your software, you can use the `Opc.Ua.CloudLib.Client` NuGet package or integrate the source code from the SampleConsoleClient found in this repo.

### Client Library Installation

```bash
dotnet add package Opc.Ua.Cloud.Library.Client
```

### Authentication Options

The client library supports multiple authentication methods:

#### 1. API Key Authentication (Recommended)

```csharp
using Opc.Ua.Cloud.Client;

// With custom endpoint
var client = new UACloudLibClient(
    "https://uacloudlibrary.opcfoundation.org",
    "CLxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxx"
);

// With default OPC Foundation endpoint
var client = new UACloudLibClient("CLxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxx");
```

API keys come in two types:
- **Read-Only**: For browsing, searching, and downloading nodesets
- **Read-Write**: For all operations including uploading and modifying nodesets

#### 2. Basic Authentication

```csharp
var client = new UACloudLibClient(
    "https://uacloudlibrary.opcfoundation.org",
    "username",
    "password"
);
```

For complete documentation and examples, see the [Client Library README](Opc.Ua.CloudLib.Client/README.md).

**Warning:** In the latest version of the REST API, a new infomodel/find2 API is introduced, returning a [UANameSpace](https://raw.githubusercontent.com/OPCFoundation/UA-CloudLibrary/refs/heads/main/Opc.Ua.CloudLib.Client/Models/UANameSpace.cs) structure, to align it with the rest of the REST API.

## Development Setup

Start development in a few simple steps:

1. Checkout ``git clone https://github.com/OPCFoundation/UA-CloudLibrary.git``
2. Copy ``.env.example`` to ``.env`` and set your own credentials. ``docker compose`` reads this file and injects the values into ``docker-compose.yml``. The ``.env`` file is git-ignored so credentials are never committed.
3. Open with Visual Studio 2019+
4. Select ``docker-compose`` as startup project and hit F5 or the "play button"

The OPC UA CloudLib Website opens in the browser.

If you want to access the database via the PG Admin tool ([PGAdmin](https://www.pgadmin.org)) for the development database instance, open http://localhost:8088/ in your browser. You will need to register a new server in PGAdmin with the following settings:
* Name: uacloudlib
* Host name/address: db
* Port: 5432
* Maintenance database: uacloudlib
* Username: uacloudlib
* Password: uacloudlib

## Authentication and Authorization
UA Cloud Library supports several authentication and authorization mechanisms. For access via the built-in UI, ASP.Net Core Identity is used and users can self-register using their email address, which needs to be verified. In addition, access to the UI via Azure Entra ID or Microsoft accounts can be optionally enabled via environment variables. Finally, the OPC Foundation hosted instance of the UA Cloud Library also supports access to the UI via OAuth and the OPC Foundation website user accounts. Access to the Swagger UI is also handled via ASP.Net Core Identity and users don't need to authenticate again once they are logged into the UI. The admin user account is enabled via the `ServicePassword` environment variable (see below).
Access to the REST API is handled via 1 default and 3 optional mechanisms:
* Basic authentication using the ASP.Net Core Identity user accounts. This is the default mechanism.
* Basic authentication using Azure Entra ID or Microsoft accounts, if enabled via environment variables.
* OAuth using the OPC Foundation website user accounts, if enabled via environment variables.
* API keys for service-to-service communication, if enabled via environment variables. API keys can then be created and managed via the UI.

There are only two types of user authorization policies supported by the UA Cloud Library: The Admin user and all other users. The Admin user has full access to all functionality, including user management, approving freshly uploaded OPC UA Information Models for download by everyone and deleting existing OPC UA Information Models. Users can upload, download, search, browse, and author OPC UA Information Models.

**Note: Custom roles can be added to users by the Admin user, if required by a calling service.**

Approval of freshly uploaded OPC UA Information Models for download by everyone can be completed via the REST API.

## Digital Product Passport (DPP)

The UA Cloud Library hosts a Digital Product Passport (DPP) Lifecycle API that exposes selected OPC UA information models as DPPs. It is aligned with three European Norms:

* **EN 18221** &mdash; *Digital Product Passport - Data storage, archiving, and data persistence* &mdash; shapes the storage, archiving and version-retrieval behaviour (Clause 4.1 storage, Clause 4.2 archiving).
* **EN 18222** &mdash; *Digital Product Passport - Application Programming Interfaces (APIs) for the product passport lifecycle management and searchability* &mdash; shapes the REST surface (`ReadDppById`, `ReadDppByProductId`, `ReadDppIdsByProductIds`, `ReadDataElement`, `UpdateDppById`, `UpdateDataElement`, `ReadDppVersionByIdAndDate`).
* **EN 18223** &mdash; *Digital Product Passport - System interoperability* &mdash; shapes the semantic data model (Clause 4: `DigitalProductPassport` and its `DataElement` subclasses) and the JSON serialization (Clause 5 / Annex A).
* **EN 18239** &mdash; *Digital Product Passport - Access rights, IT security and business confidentiality* &mdash; shapes the public/controlled access split, element-level role-based access control and operator identification (Clause 5.2).
* **EN 18246** &mdash; *Digital Product Passport - Data authentication, reliability and integrity* &mdash; shapes the tamper-evident audit log (Clause 4.7), Electronic Signed Data Constructs (Clause 4.5) and unauthenticated public read (Clause 5.1).

### Conceptual model

A DPP is constructed on-demand from an OPC UA nodeset that has been uploaded to the Cloud Library. The DPP root, its `uniqueProductIdentifier`, `granularity`, `dppStatus`, `lastUpdate` timestamp, `economicOperatorId`/`facilityId`, `contentSpecificationIds`, and the tree of `DataElement` children are read from the live OPC UA address space rooted at the `Objects` folder of the addressed nodeset. The DPP identifier (`dppId`) is the nodeset identifier issued by the Cloud Library on upload.

The DPP header follows EN 18223 Clause 4.1.2.1 Table 1:

| Property | Cardinality | Notes |
|---|---|---|
| `digitalProductPassportId` | [1] | Globally unique, opaque string. EN 18223 does not mandate a URI shape; this server emits the Cloud Library nodeset identifier (the decimal form of the nodeset's stable hash code, e.g. `"3851629631"`) so callers can round-trip the value back through the `v1/dpps/{dppId}` endpoints. |
| `uniqueProductIdentifier` | [1] | Product identifier per EN 18219. |
| `granularity` | [1] | Enumeration: `model`, `batch`, `item` (EN 18223 Clause 4.1.2.2 — lowercase on the wire). |
| `dppSchemaVersion` | [1] | Reference standard the DPP schema follows. |
| `dppStatus` | [1] | e.g. `active`, `inactive`, `archived`, `invalid`. |
| `lastUpdate` | [1] | UTC timestamp per ISO 8601-1. |
| `economicOperatorId` | [1] | Operator identifier per EN 18219. |
| `facilityId` | [0..1] | Facility identifier per EN 18219. |
| `contentSpecificationIds` | [0..*] | References to horizontal or product-type content specifications. |
| `elements` | [0..*] | Tree of `DataElement` instances. |

`DataElement` is a polymorphic type discriminated by the `objectType` property (EN 18223 Clause 4.1.2.3 - 4.1.2.8):

| `objectType` | C# type | Purpose |
|---|---|---|
| `DataElementCollection` | `DataElementCollection` | A named container of child `DataElement`s (mixed types allowed). |
| `SingleValuedDataElement` | `SingleValuedDataElement` | A leaf carrying a single value (any JSON primitive, object or array). |
| `MultiValuedDataElement` | `MultiValuedDataElement` | A leaf carrying a homogenous, non-empty list of nested `DataElement`s (children serialized under `value`). |
| `RelatedResource` | `RelatedResource` | A reference to an external resource (document, certificate) with `contentType`, `url`, optional `language` and `resourceTitle`. |
| `MultiLanguageDataElement` | `MultiLanguageDataElement` | A language-dependent value with one or more `{ value, language }` entries under `value`. |

> **`elementId` is a server-generated GUID.** When a DPP is materialised from its OPC UA nodeset, each element's `elementId` is a stable GUID derived (via SHA-256) from the node's `ExpandedNodeId`. Because the `ExpandedNodeId` embeds the namespace URI plus the node identifier, the id is globally unique even when BrowseNames/DisplayNames collide across namespaces, and it is deterministic across reads (the same element always gets the same id), so element addressing (`elementIdPath`) and the per-DPP access mapping stay stable. The illustrative payloads below use readable ids (e.g. `maxPressure`) for legibility, matching the EN 18223 Annex A examples; a live response carries GUID `elementId`s. The element's human/semantic meaning is conveyed by its `dictionaryReference` (e.g. IEC CDD), not by the id.

### Serialization (EN 18223 Clause 5 / Annex A)

EN 18223 defines two equivalent JSON serializations:

* a **compressed** form (Clause 5.2) where each `DataElement` uses its `elementId` as the JSON object key and `dictionaryReference`/`valueDataType` are looked up from an external data dictionary, and
* an **expanded** form (Annex A) where every `DataElement` is a self-describing JSON object carrying `objectType`, `elementId`, optional `dictionaryReference`, optional `valueDataType` and the value/children of the element.

This implementation emits the **expanded form** in every response from [`DPPLifecycleApiController`](UACloudLibraryServer/Controllers/DPPLifecycleApiController.cs) and accepts the same expanded form on `PATCH` requests. This choice keeps DPP payloads self-describing and removes the need for the client to resolve dictionary references in order to interpret a value. The expanded form is structurally identical to the examples shown in EN 18223 Annex A.

The discriminator property is `objectType` (EN 18223 Clause 5.2.2 / Annex A). The mapping between the EN 18223 subclasses and the JSON shape produced by [`DppModel.cs`](UACloudLibraryServer/Models/DppModel.cs) is:

| EN 18223 subclass | Clause | JSON shape |
|---|---|---|
| `DigitalProductPassport` | 4.1.2.1 / 5.2.4 | Top-level object with the header properties above and an `elements` array. |
| `DataElementCollection` | 4.1.2.4 / 5.2.5 | `{ "objectType": "DataElementCollection", "elementId": ..., "elements": [ ... ] }`. |
| `SingleValuedDataElement` | 4.1.2.5 / 5.2.6 | `{ "objectType": "SingleValuedDataElement", "elementId": ..., "valueDataType": ..., "value": <any JSON type> }`. |
| `MultiValuedDataElement` | 4.1.2.6 / 5.2.7 | `{ "objectType": "MultiValuedDataElement", "elementId": ..., "valueDataType": ..., "value": [ ...same-type DataElements... ] }` (children under `value`, per Annex A Example 4). |
| `RelatedResource` | 4.1.2.7 / 5.2.8 | `{ "objectType": "RelatedResource", "elementId": ..., "contentType": ..., "url": ..., "language": ..., "resourceTitle": ... }`. |
| `MultiLanguageDataElement` | 4.1.2.8 / 5.2.9 | `{ "objectType": "MultiLanguageDataElement", "elementId": ..., "value": [ { "value": "...", "language": "en-GB" }, ... ] }`. |

`valueDataType` values follow the XSD-to-JSON mapping table of EN 18223 Clause 5.2.3 (e.g. `xsd:integer`, `xsd:decimal`, `xsd:boolean`, `xsd:string`, `xsd:dateTime`, `xsd:anyURI`, `xsd:base64Binary`). The unsupported XSD types listed in Clause 4.1.2.9 (`ENTITIES`, `IDREFS`, `NMTOKENS`, `NOTATION`, `QName` and the XSD `string`-derived built-ins) are also not produced by this server.

Example DPP body returned by `GET v1/dpps/{dppId}` (abbreviated, expanded form):

```json
{
  "digitalProductPassportId": "3851629631",
  "uniqueProductIdentifier": "https://example.org/products/abc",
  "granularity": "model",
  "dppSchemaVersion": "EN18223:v1.0",
  "dppStatus": "active",
  "lastUpdate": "2025-08-22T03:12:00Z",
  "economicOperatorId": "gxx:ppp456789",
  "facilityId": "gxx:xxx987654",
  "contentSpecificationIds": ["EN1234_xyz", "EN5678_abc"],
  "elements": [
    {
      "objectType": "DataElementCollection",
      "elementId": "performanceMetrics",
      "elements": [
        {
          "objectType": "SingleValuedDataElement",
          "elementId": "maxPressure",
          "valueDataType": "xsd:float",
          "value": 750.0
        },
        {
          "objectType": "MultiValuedDataElement",
          "elementId": "efficiencyRatings",
          "valueDataType": "xsd:float",
          "value": [
            { "objectType": "SingleValuedDataElement", "elementId": "r1", "valueDataType": "xsd:float", "value": 0.95 },
            { "objectType": "SingleValuedDataElement", "elementId": "r2", "valueDataType": "xsd:float", "value": 0.92 }
          ]
        }
      ]
    },
    {
      "objectType": "MultiLanguageDataElement",
      "elementId": "productDescription",
      "value": [
        { "value": "Smart Thermostat", "language": "en-GB" },
        { "value": "Intelligenter Thermostat", "language": "de-DE" }
      ]
    },
    {
      "objectType": "RelatedResource",
      "elementId": "userManual",
      "contentType": "application/pdf",
      "url": "https://data.example.com/manuals/thermostat.pdf",
      "language": "en-GB",
      "resourceTitle": "User Manual"
    }
  ]
}
```

### Response envelope

Every endpoint returns the same envelope (`ApiResponse<T>` in `Models/DppApiResponse.cs`):

```jsonc
{
  "statusCode": "Success",                 // DppApiStatusCodes constant
  "payload":    { /* T */ },               // method-specific result, may be null on error
  "result":     { "message": [             // optional human-readable messages
      { "messageType": "Error", "text": "Resource not found" }
  ]},
  "pagination": { "nextCursor": "20", "hasMore": true, "limit": 20 } // only on paged methods
}
```

The `statusCode` values are the symbolic constants from `DppApiStatusCodes` (`Success`, `ClientErrorBadRequest`, `ClientErrorResourceNotFound`, `ServerInternalError`, ...). The HTTP status code mirrors the envelope status (`200`, `400`, `404`, `500`).

### Endpoints

All routes are versioned under `v1/`. The **read** routes (`GET dpps/{dppId}`, `GET dppsByProductId/{productId}`, `POST dppsByProductIds`, `GET dpps/{dppId}/elements/{*elementIdPath}`, `GET dpps/{dppId}/versions/{date}`) are reachable **anonymously** so public DPP data can be read without login (EN 18246 Clause 5.1); authenticated callers additionally see any controlled elements their roles permit. The **write** routes (`PATCH`) still require an authenticated principal satisfying the `ApiPolicy` (Basic auth, signed-in cookie, or `X-API-Key` &mdash; see [Authentication and Authorization](#authentication-and-authorization)). See [Access control, audit and signing](#access-control-audit-and-signing-en-18239--en-18246) for how controlled elements are filtered.

| Method | Route | DPP lifecycle operation |
|---|---|---|
| `GET`   | `v1/dpps/{dppId}` | Returns the full DPP rooted at the addressed nodeset. Public; controlled elements are filtered out for callers lacking the role. |
| `GET`   | `v1/dppsByProductId/{productId}` | Returns the latest DPP whose `UniqueProductIdentifier` equals `productId` (resolved by browsing all nodesets visible to the caller and picking the newest `lastUpdate`). |
| `POST`  | `v1/dppsByProductIds` | Returns the list of DPP identifiers matching the supplied `productIds`. Supports paging via `?limit=` and `?cursor=` query parameters; the response envelope carries a `pagination` block. |
| `GET`   | `v1/dpps/{dppId}/elements/{*elementIdPath}` | Returns the `DataElement` addressed by the JSONPath subset described below. |
| `PATCH` | `v1/dpps/{dppId}` | Applies a partial DPP with merge-patch-shaped semantics: only members present in the request body are touched, members that are absent are left unchanged. Full RFC 7396 deletion (`null` means "delete that field") is **not** supported because the DPP is backed by a fixed OPC UA address space, so `null` on any scalar field and any non-array value for `elements` are rejected as `400`. Accepts `application/json`. Snapshots the pre-update DPP into the archive, writes leaf values to the live OPC UA server, then persists the new values. |
| `PATCH` | `v1/dpps/{dppId}/elements/{*elementIdPath}` | Updates a single addressed leaf element. Snapshots, writes, and persists as above. |
| `GET`   | `v1/dpps/{dppId}/versions/{date}` | Returns the DPP snapshot that was active at the supplied ISO 8601 timestamp. If `date` is &ge; the live DPP's own `LastUpdate`, the live DPP is returned; otherwise the archive is consulted for the latest snapshot at or before `date`. Returns `404` when no version existed at that point in time. |

The request body for `POST v1/dppsByProductIds` is:

```json
{ "productIds": ["urn:product:1", "urn:product:2"] }
```

### `elementIdPath` &mdash; JSONPath addressing

`elementIdPath` is a JSONPath expression rooted at the `DigitalProductPassport.elements` collection. The parser ([`DppJsonPath`](UACloudLibraryServer/DPP/DppJsonPath.cs)) accepts the subset of RFC 9535 actually used by the DPP data model:

* Optional root identifier `$` or `$.`.
* Dot child selector: `.name`.
* Bracket name selector with single or double quotes: `['name']`, `["name"]`.
* Bracket index selector: `[0]`, `[3]`.

Filter expressions, wildcards (`*`), slice selectors and the descendant operator (`..`) are rejected with `ClientErrorBadRequest`. Examples:

```
manufacturer
materials[0].name
$['battery']['cells'][2]['voltage']
```

### Write semantics, archival and persistence

The DPP update path strictly separates the three concerns of the Browser UI's `Save` flow:

1. **Pre-update snapshot capture.** Before any write, `DPPService` browses the current DPP and keeps the snapshot in memory.
2. **Live write.** The resolved leaf values are written to the running embedded OPC UA server via `UAClient.VariableWrite(...)`. `UAClient` does *not* persist anything to the database.
3. **Explicit persistence.** After the live write succeeds, `DPPService.PersistNodesetValuesAsync(...)` re-browses the variables and upserts the serialized values into `DbFiles.Values` through `DbFileStorage.UploadFileAsync(...)`, so the change survives a server restart (the embedded OPC UA server rehydrates values from `DbFiles.Values` on startup via `NodesetFileNodeManager.AddNodesAndValues`).
4. **Archive commit.** Only after the live write **and** persistence both succeed does `DPPService` call `IDppVersionArchive.ArchiveAsync(dppId, snapshot, snapshot.LastUpdate.ToUniversalTime())` with the pre-update snapshot captured in step 1. The capture timestamp is the snapshot's own `LastUpdate` (i.e. when that version *became active*) rather than `DateTimeOffset.UtcNow` (which would record when the *next* version takes over), so the archive's at-or-before lookup in `GetVersionAtAsync` correctly returns the snapshot for any `asOfUtc` inside its validity window. Failed updates therefore never create phantom archive entries, and the archive view is always consistent with what was actually persisted. This satisfies the EN 18221 Clause 4.2 requirement that *“archiving starts when the first change of the initial digital product passport occurs”* and that *“all changes to the digital product passport shall be archived”*.

No-op updates (an empty PATCH body, or a body whose entries all resolve to zero concrete writes) short-circuit before step 2, so they never touch the OPC UA address space, never bump the persisted `PublicationDate`, and never create an archive entry.

**Rollback on archive failure.** If pre-update snapshot capture fails (returns null) or archive commit fails after persistence, the entire update is rolled back: `DPPService` restores the already-applied writes to their captured original values and re-persists, then returns `WriteFailed` (500). This keeps the observable API outcome synchronized with the stored state and prevents clients from seeing a failure for a durable update and inadvertently retrying (which would apply the update twice). The only residual failure case where the DPP can stay partially mutated is when the compensating rollback writes themselves fail, which is logged so operators can reconcile manually.

To prevent a save from silently overwriting an earlier on-disk version, the persistence step rewrites the `PublicationDate="..."` attribute in the stored nodeset XML to the current UTC timestamp with millisecond precision (`yyyy-MM-ddTHH:mm:ss.fffZ`). This mirrors the in-XML date bump already used by `UAClient.CopyNodeset` and ensures every persisted save is uniquely datable even when updates land in the same wall-clock second.

When the update payload addresses an element via `value`, the server decides leaf-vs-collection semantics from the **live OPC UA browse** of the matched node, not from the client-supplied `objectType` field. If the live node has children the array under `value` is recursed into (multivalued collection); if it has no children the array is written as-is (multilanguage leaf). This keeps a malicious or buggy client from forcing an array payload onto the wrong parent node.

DPP leaf values are persisted by the OPC UA layer as strings. The read path only re-types values whose stored text starts with an unambiguous JSON **structural** marker (`{`, `[` or `"`): JSON objects, arrays and quoted strings round-trip as their typed `JsonNode` form, while everything else surfaces verbatim as a JSON string. Numeric, boolean and bare-`null` literals are intentionally **not** re-typed because there is no per-leaf type metadata to tell e.g. the product code `"007"` apart from the number `7`, or the stored string `"true"` apart from the boolean `true`. Clients that need typed scalars should write them inside an explicit JSON object / array shape (e.g. a `MultiValuedDataElement.value` entry) and parse leaf strings themselves when needed.

### Durable version archive

The archive implements the archiving rules of EN 18221 Clause 4.2 (point-in-time retrievability of all past changes during the DPP lifetime) by reusing the existing `DbFileStorage`:

* Each snapshot is stored as its own row in the `DbFiles` table whose `Name` follows the layout `dpp-archive::{dppId}::{capturedAtUtcTicks:D19}-{counter:X6}{randomHex8}` and whose `Blob` holds the JSON-serialized `DigitalProductPassport`. The trailing `-{counter:X6}{randomHex8}` segment combines a per-process monotonic counter (formatted as 6 uppercase hex chars, masked to 24 bits to keep the field fixed-width) with 4 random bytes (8 hex chars) so each row name is probabilistically unique by construction, both within and across processes.
* The fixed-width 19-digit tick stamp remains the dominant sort key, so lexicographic ordering still matches chronological order: retrieving the snapshot at or before a target timestamp is an ordered prefix scan via `DbFileStorage.ListFileNamesAsync(prefix)`. Ties within the same tick are broken deterministically by `(counter, randomHex)`, so the scan still selects the most recent write at that tick.
* Because the row name is probabilistically unique, the previous check-then-write loop has been removed. The underlying writer (`DbFileStorage.UploadFileAsync`) is an upsert keyed on `DbFiles.Name`, so an astronomically unlikely `(counter, randomHex)` collision within the same tick would silently overwrite the prior row rather than being rejected; the collision odds (~1 in 2^32 per same-tick same-counter pair) sit well below the practical concern threshold for an archive workload, and the archived tick value always reflects the snapshot's true capture time.

`DbFileVersionArchive` is registered as a scoped service in `Startup.ConfigureServices`. Its dependency `DbFileStorage` (and the underlying `AppDbContext`) are registered as transient, so each archive instance gets a fresh storage layer scoped to the current HTTP request without sharing change-tracker state across requests.

### Access control, audit and signing (EN 18239 / EN 18246)

Three security capabilities sit across the read and write paths. Each is keyed off an injectable service so the policy can change without touching the data model.

**Public vs. controlled access (EN 18239 Clause 5.2).** Read access is *public by default*. Whether a `DataElement` is controlled is decided from its **path** (its dotted `elementId` address, the same one the `elementIdPath` API uses) &mdash; deliberately *not* from its `dictionaryReference`, which EN 18223 §4.3 reserves for semantic dictionary references (IEC CDD, ECLASS, EU Vocabularies) and is unsuitable as an access key. Since each `elementId` is a server-generated stable GUID (see the note above), a path is a chain of GUIDs that you obtain by reading the DPP once and then reference in the mapping. The mapping is supplied **per DPP**: the values JSON uploaded alongside the nodeset (via `/infomodel/upload` or the upload UI) may carry a reserved `controlledElements` object at the end, mapping each element path to the role (or roles) permitted to read that element and its subtree (a controlled container path controls everything beneath it). The values file is otherwise the flat `{ nodeId: value }` map already used to seed variable values:

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

[`DppControlledElements`](UACloudLibraryServer/DPP/DppControlledElements.cs) parses this object out of the DPP's stored values blob ([`IDppAccessPolicy`](UACloudLibraryServer/DPP/IDppAccessPolicy.cs) / [`DppAccessPolicy`](UACloudLibraryServer/DPP/DppAccessPolicy.cs) then evaluates it by element path, applying prefix/subtree semantics).

**Tamper-evident audit log (EN 18246 Clause 4.7, EN 18239 §5.2(16)).** Every read (`GET`) and modify (`PATCH`) on a DPP, and every role/rights change, is recorded by [`IDppAuditLog`](UACloudLibraryServer/DPP/IDppAuditLog.cs) / [`DppAuditLog`](UACloudLibraryServer/DPP/DppAuditLog.cs) and bound to the acting operator id. Entries are SHA-256 hash-chained (each row hashes its content plus the previous hash), so any retrospective insert, edit or delete breaks the chain; `VerifyChainAsync` re-walks the chain and additionally compares it against a persisted checkpoint (`DppAuditCheckpoints`, migration `AddDppAuditCheckpointAndEsdcSigningKey`) so that deleting the most recent entries &mdash; which would otherwise leave a still-valid prefix &mdash; is also detected. Rows persist in the `DppAuditEntries` table (migration `AddDppAuditLog`). Each field is length-prefixed before hashing rather than joined with a separator, so caller-supplied values containing the separator cannot produce two different entries with the same hash. Appends run in a serializable transaction so concurrent writers, including separate instances, cannot branch the chain. If an entry cannot be durably committed the request fails with `503` rather than completing unlogged, because returning success for an access that was never recorded would forfeit the non-repudiation guarantee the log exists to provide.

> **Audit durability limitation.** A DPP mutation touches the OPC UA address space, the version archive and the audit table &mdash; three stores with no shared transaction. Mutations therefore write an `Attempted` audit entry *before* the change and a `Success` entry after it. A failed pre-write append refuses the request before anything is modified; an `Attempted` entry with no matching outcome means a change may have completed without being fully logged, and is the signal to investigate. This makes the gap **detectable**, not impossible: genuine atomicity requires a transactional outbox, which is not yet implemented.

> **Audit reads are logged before data is produced.** On read paths the audit entry is appended *before* the ESDC is issued and before the response is constructed. The ESDC embeds the full DPP, so producing it first would leave the data on a response that a failed audit append then turns into a `503` &mdash; a refused, unaudited read would still have handed over the data.

> **Timestamp precision.** Audit timestamps are truncated to microseconds before being hashed, because PostgreSQL's `timestamp with time zone` stores microseconds while `DateTimeOffset.UtcNow` carries 100-nanosecond ticks. Hashing the untruncated value would produce a hash the database can never reproduce, so verification would report tampering for untouched entries after the first reload. **Entries written before this change were hashed from untruncated timestamps and will not re-verify**; treat an existing chain as valid only from the first entry written after upgrading.

> **What the audit log does and does not prove.** Configure `Dpp:Audit:HmacKey` (base64, at least 32 bytes, from a managed secret store) to authenticate the chain with HMAC-SHA256. This matters because the chain and its checkpoint live in the same database they are meant to protect: an *unkeyed* digest is a public function of the stored rows, so anyone able to edit those rows can simply recompute the digests and `VerifyChainAsync` will still return true. Keying means an attacker needs the key as well as database write access.
>
> Be clear about the remaining limit: this does **not** defend against an attacker who holds both the key and database write access, and it does not prove *when* an entry was written. A deployment that needs tamper evidence against a compromised database operator should additionally anchor the checkpoint in an external append-only store (a WORM bucket, a transparency log, or periodic off-host export). That is not implemented here.
>
> When no key is configured the server logs a warning at startup and the log degrades to an integrity check only &mdash; it will still catch accidental corruption, but not deliberate modification. **Entries written under a different key (or none) will not re-verify after the key changes**, for the same reason as the timestamp change above.

**Electronic Signed Data Constructs (EN 18246 Annex A / B.5, §4.7).** Full-DPP reads return the ESDC in the response envelope's `esdc` field, carrying it as a **W3C Verifiable Credential** (VC Data Model 2.0) secured with an enveloped JWS (`application/vc+jwt`) per the W3C *Securing Verifiable Credentials using JOSE and COSE* recommendation. The DPP is the credential's `credentialSubject.digitalProductPassport`, the economic operator is the `issuer`, and the unique product identifier is the subject `id`. [`IEsdcService`](UACloudLibraryServer/DPP/IEsdcService.cs) / [`RsaEsdcService`](UACloudLibraryServer/DPP/RsaEsdcService.cs) sign with RS256, using the key resolved by [`IEsdcSigningKeyProvider`](UACloudLibraryServer/DPP/IEsdcSigningKeyProvider.cs) (see [ESDC signing key management](#esdc-signing-key-management)); an optional issuer certificate (`Dpp:Esdc:CertificatePem`) is embedded in the JWS header (`x5c`) and must correspond to the signing key, or the server refuses to start &mdash; a mismatched certificate would tell verifiers that a key which cannot verify the signature produced the credential. The VC-JWT is self-contained and independently verifiable by any VC-JWT verifier, free of charge and without contacting the issuer. `Verify` accepts only signatures made by a **trusted** key &mdash; this server's own key or one listed in `Dpp:Esdc:TrustedPublicKeysPem` &mdash; and additionally requires the unsigned envelope fields to match the signed credential; a key embedded in a submitted ESDC is never trusted, since that would prove only self-consistency. Validating the issuer's certificate against an EU trusted list / governance framework (Annex A.3) is a deployment responsibility and is out of scope of this service.

> **ESDC delivery.** The signed artifact embeds the entire DPP, so it is returned in the response body rather than a header: base64-encoding it into a single header readily exceeds common server and proxy header limits, which would make otherwise valid full-DPP reads fail operationally. A compact `X-DPP-ESDC-KeyId` header carries only the key id, so a verifier can select its trust anchor without parsing the body. The ESDC is a sibling of `payload` rather than part of it, because it signs the payload.

### ESDC signing key management

The signing key is the sole basis on which a verifier decides an ESDC is authentic, so it **must be stable**. A key that changes between restarts, or differs between instances behind a load balancer, silently invalidates every ESDC issued under the previous key.

[`EsdcSigningKeyProvider`](UACloudLibraryServer/DPP/EsdcSigningKeyProvider.cs) resolves the key in this order:

| Order | Source | When to use |
| --- | --- | --- |
| 1 | `Dpp:Esdc:PrivateKeyPem` | **Required in production.** Supplied from a managed secret store; never stored in application data. |
| 2 | `EsdcSigningKeys` table | Reused automatically once generated, so the key survives restarts and is shared by all instances. |
| 3 | Newly generated RSA-2048 | Only when neither of the above exists, **and** only in Development or with `Dpp:Esdc:AllowGeneratedSigningKey=true`. |

> **The server fails to start** outside the Development environment if no key is configured and `Dpp:Esdc:AllowGeneratedSigningKey` is not set to `true`. Signing production ESDCs with a key held in application data is a deliberate downgrade, so it has to be a decision you made on purpose &mdash; not one the server made quietly on your behalf. Starting cleanly and only revealing the problem when the first DPP is read would be worse: by then the weak key is already in use and in your backups.

#### Generating a key

```bash
# Private key (PKCS#8 PEM) - keep secret
openssl genpkey -algorithm RSA -pkeyopt rsa_keygen_bits:2048 -out esdc-signing-key.pem

# Public key - safe to publish; this is what verifiers configure as a trust anchor
openssl rsa -pubout -in esdc-signing-key.pem -out esdc-public-key.pem
```

Optionally issue a certificate over the same key and supply it as `Dpp:Esdc:CertificatePem`; it is embedded as `x5c` so verifiers can chain the key to an accredited economic operator.

#### Storing it securely

Provide the PEM through configuration &mdash; never commit it to source control or bake it into a container image:

- **Azure Key Vault** (recommended for the hosted deployment): store as secret `Dpp--Esdc--PrivateKeyPem` and reference it from App Service/Container Apps, or load it with the Key Vault configuration provider.
- **Environment variable**: `Dpp__Esdc__PrivateKeyPem` (double underscores map to the `:` hierarchy).
- **Kubernetes**: mount a `Secret` and project it into that environment variable.
- **Local development**: `dotnet user-secrets set "Dpp:Esdc:PrivateKeyPem" "$(cat esdc-signing-key.pem)"` &mdash; keeps it out of `appsettings.json`.

Restrict read access to the secret to the application identity, and rely on the secret store's own audit log to record access.

> **On the generated fallback key.** If no key is configured, the server generates one and stores it in the `EsdcSigningKeys` table (migration `AddDppAuditCheckpointAndEsdcSigningKey`). This keeps ESDCs verifiable across restarts and replicas, which an in-memory key cannot do &mdash; but the private key is then present in every database backup and readable by anything with database access. It is intended for development and evaluation. Outside Development it is refused unless you set `Dpp:Esdc:AllowGeneratedSigningKey=true`, and even then the server logs a warning at startup naming the environment it is running in.

#### Rotation

Rotating the key changes `KeyId` and invalidates ESDCs signed with the old key, so treat it as a coordinated change:

1. Generate a new key pair and distribute the **new public key** to verifiers as an additional trust anchor.
2. Keep the old public key in their `Dpp:Esdc:TrustedPublicKeysPem` list until previously issued ESDCs are no longer relied upon &mdash; both keys can be trusted simultaneously.
3. Update `Dpp:Esdc:PrivateKeyPem` and restart. A configured key always takes precedence, so this also cleanly supersedes a previously generated fallback key.
4. Once the old key is retired, remove it from the verifiers' trust anchors.

To verify ESDCs issued by *other* economic operators, bind each trusted key to the issuer it is authorized to sign for:

```json
"Dpp": {
  "Esdc": {
    "TrustedIssuers": [
      { "Issuer": "EO-12345", "PublicKeyPem": "-----BEGIN PUBLIC KEY-----\n...", "KeyId": "optional-kid" }
    ]
  }
}
```

`Verify` selects the anchor from the **signed** `issuer` (and `kid`, when pinned) before checking the signature. The binding is the point: a bare list of trusted keys establishes only that *someone* trusted signed the credential, so any trusted peer could issue one naming a different economic operator and it would still verify. An anchor configured without an `Issuer` fails startup, and a malformed key PEM fails startup rather than silently narrowing trust.

The older flat `Dpp:Esdc:TrustedPublicKeysPem` list is still read so existing deployments keep working, but those keys carry no issuer binding and are refused whenever a bound anchor already claims the signed issuer &mdash; so adding a bound anchor for an operator stops an unbound key impersonating it. Prefer `TrustedIssuers` for new configuration.


**Access-rights management & revocation (EN 18239 §5.2(16)/(17)/(19), §6.3).** Roles and their assignments are managed through [`AccessController`](UACloudLibraryServer/Controllers/AccessController.cs) (admin-only, `AdministrationPolicy`): `PUT`/`DELETE /access/roles/{roleName}` create/delete a role, and `PUT`/`DELETE /access/userRoles/{userId}/{roleName}` grant/revoke a role for an actor. The `DELETE` routes provide the documented access-revocation process and emergency revocation on breach or non-compliance. Every grant, revoke, create and delete is written to the tamper-evident audit log, bound to the acting administrator.

> **The administrator role is protected.** Deleting the `Administrator` role, or revoking it from the last remaining administrator, is refused with `403`. Both would be unrecoverable in-band: deleting the role drops the `AspNetUserRoles` links for every administrator and recreating it does not restore them, so the only accounts able to call these endpoints and repair the damage would have just lost that ability. Revoking from one of several administrators remains allowed, so emergency revocation still works.

**Abuse prevention / rate limiting (EN 18239 §5.2(11)/(15)).** The DPP endpoints are guarded by a built-in ASP.NET Core rate limiter (`DppRateLimitPolicy`, registered in [`Startup`](UACloudLibraryServer/Startup.cs)) that partitions a fixed window per client IP and returns `429 Too Many Requests` when exceeded. The per-minute permit count is configurable via `Dpp:RateLimit:PermitPerMinute` (default 100), letting an economic operator tighten limits where identified risk or legal requirements warrant.

### Standards conformance (EN 18239 / EN 18246)

The DPP service implements the access-rights, security and data-authentication requirements as summarised below. Items marked *Deployment* are organisational, identity-provider or infrastructure responsibilities outside this application's code.

**EN 18239 — Access rights management, IT security, business confidentiality**

| Clause | Requirement | Status |
|---|---|---|
| §5.2(2), §6.1/6.2 | Public data readable without authentication; unauthenticated public vs. authenticated controlled access schemes | Implemented |
| §5.2(1)/(4)/(8) | Globally unique operator identifier available for identification/authorization/accounting | Implemented |
| §5.2(5)/(16) | Non-repudiation; all access and role/rights changes logged and tamper-evident over time | Implemented |
| §5.2(10)/(23) | Access rights enforceable at controlled data-element granularity per role | Implemented |
| §5.2(11)/(15) | Limit access to prevent attacks / unauthorized mass data scraping (rate limiting) | Implemented |
| §5.2(17)/(19), §6.3 | Granting/modifying/revoking access when roles change; emergency revocation; access-revocation policy | Implemented |
| §5.2(22) | Controlled data authenticated and authorized before access; least privilege | Implemented |
| §5.2(24) | Profiling of users avoided | Implemented |
| §5.2(18)/(20) | Delegation of roles (authorization also considering the delegating entity's role) | Deployment |
| §5.2(13), Annex A | Authentication per the relevant legal act; MFA, identity proofing, sole control, dynamic auth | Deployment |
| §6.4 / §6.5 | Business continuity (ISO 22301), ISMS/PDCA (ISO 27001), incident response, DoS resilience, security-by-design | Deployment |

**EN 18246 — Data authentication, reliability and integrity**

| Clause | Requirement | Status |
|---|---|---|
| §4.3 / §5.1 | Public read without authentication and without collecting PII | Implemented |
| §4.7 | Create/modify on DPP logged non-repudiably, tamper-proof, integrity preserved over time | Implemented |
| §4.5, Annex A | ESDC issuance and verification (W3C Verifiable Credential, `application/vc+jwt`) | Implemented |
| §4.7, Annex A.3 | Authenticity/integrity verifiable independently, free of charge, without contacting the issuer | Implemented |
| Annex A.3 (4th bullet) | Validating the issuer's certificate against an EU trusted list / governance framework | Deployment |

The *Deployment* rows are deliberately left to the operator/host because they depend on the chosen identity provider (e.g. eIDAS-compliant MFA and identity proofing), an EU trusted-list infrastructure, and organisational ISMS/BCM processes rather than application logic.

### Error responses

| Condition | HTTP | `statusCode` |
|---|---|---|
| Resource (DPP or element) does not exist | 404 | `ClientErrorResourceNotFound` |
| Invalid request body, malformed `elementIdPath`, bad `date`, bad pagination input | 400 | `ClientErrorBadRequest` |
| Live OPC UA write or post-write persistence failed | 500 | `ServerInternalError` |
| Method succeeded | 200 | `Success` |

## Database Configuration
The UA Cloud Library database configuration is documented in the [Database Setup](Docs/Database%20Setup.md) document.

## Cloud Hosting Setup

### Migrating from version 1.0 to version 1.1
To migrate from version 1.0 to version 1.1, you need to update your database as V1.1 no longer requires blob storage. We provided a command line tool called BlobToPGTable in this repository for the major clouds which will complete this step for you.

### Required Settings - PostgreSQL
You **must** have installed PostgreSQL version 11.20 or higher. You **must** also define the following environment variable:

* `ConnectionStrings__CloudLibraryPostgreSQL`: A full Npgsql connection string to the PostgreSQL instance (that must be previously deployed in the hosting platform). Example:
```
"Server=localhost;Username=MyUserName;Password=MyUserPassword;Database=uacloudlib;Port=5432;Ssl Mode=Require;Include Error Detail=true",
```
**Note: you must create a user account with set privileges to access the database, and set `Ssl Mode=Require` for any non-loopback server — most managed PostgreSQL offerings (including Azure Database for PostgreSQL) reject unencrypted connections outright.**

### Setting Credentials for Admin Account
To enable access, from both Swagger and the REST API, you must set a password using this environment variable:
* `ServicePassword`: The administration password for Swagger and REST service.

Optionally, set the admin user name as well:
* `ServiceUsername`: The administration user name for Swagger and REST service (default: `admin`).

### Optional Settings
Environment variables that **can optionally** be defined:

* `EmailSenderAPIKey`: The API key for the email sender service
* `RegistrationEmailFrom`: The "from" email address to use for user registration confirmation emails
* `RegistrationEmailReplyTo`: The "replyto" email address to use for user registration confirmation emails
* `AllowSelfRegistration`: Whether users can self-register for a user account (default: `true`).

### Optional Settings - Captcha
Curtail bot access using the Google reCAPTCHA.  

**Note: If you enable reCAPTCHA without an active account, it breaks user self-registration.**

* `CaptchaSettings__Enabled`: Toggle whether to use reCAPTCHA (default: `false`). 
* `CaptchaSettings__SiteVerifyUrl`: Verify user input (default: `https://www.google.com/recaptcha/api/siteverify`)
* `CaptchaSettings__ClientApiUrl`: Source for loading JavaScript library (default:`https://www.google.com/recaptcha/api.js?render=`)
* `CaptchaSettings__SecretKey`: Private key. Obtain from reCAPTCHA admin console.
* `CaptchaSettings__SiteKey`: Public key. Obtain from reCAPTCHA admin console.
* `CaptchaSettings__BotThreshold`: Minimum score between 0.0 (bot likely) and 1.0 (human likely). (default: `0.5`)

### Optional Settings - Data Protection

* `DATA_PROTECTION_KEY_PATH`: Directory holding the ASP.NET Core Data Protection key ring. The directory is created if it does not exist. (default: the process working directory)

> **Set this in any containerised deployment.** The key ring encrypts authentication cookies, antiforgery tokens and the e-mail confirmation / password-reset tokens, so it has to outlive the process. The default is the container's working directory, which is discarded on every restart &mdash; losing the keys signs every user out and makes outstanding reset links fail. The failure is also misleading rather than obvious: the antiforgery token on an already-open login page can no longer be validated, so the POST is rejected before the credentials are ever checked, which presents to the user as a wrong password. Point this at a mounted volume that survives restarts, and share it across replicas so a request handled by one instance can be decrypted by another. The key ring's purpose string is pinned via `SetApplicationName` from the `Application` setting (default `UACloudLibrary`), so that value must also stay consistent across restarts and replicas.

### Optional Settings - DPP
* `Dpp__Esdc__PrivateKeyPem`: PKCS#8 PEM private key used to sign ESDCs. **Required in production** &mdash; the server refuses to start without it unless `Dpp__Esdc__AllowGeneratedSigningKey` is `true`. See [ESDC signing key management](#esdc-signing-key-management).
* `Dpp__Esdc__AllowGeneratedSigningKey`: Set to `true` to permit a server-generated key stored in the database outside Development. (default: `false`)
* `Dpp__Esdc__CertificatePem`: Optional issuer certificate PEM, embedded in the JWS header (`x5c`).
* `Dpp__Esdc__TrustedPublicKeysPem__0`, `__1`, ...: Legacy, unbound public key PEMs of peer economic operators. Prefer the issuer-bound form below.
* `Dpp__Esdc__TrustedIssuers__0__Issuer`, `__0__PublicKeyPem`, `__0__KeyId`: Trusted peer bound to the issuer it may sign for. `KeyId` is optional and pins the JWS `kid`.
* `Dpp__Audit__HmacKey`: Base64 key (at least 32 bytes) authenticating the DPP audit chain. Without it the chain is integrity-checked but not tamper-evident against database modification.
* `Dpp__RateLimit__PermitPerMinute`: Requests permitted per minute per client IP on the DPP endpoints. (default: `100`)
* `Dpp__ForwardedHeaders__KnownProxies__0`, `__1`, ...: IP addresses of trusted reverse proxies whose `X-Forwarded-For`/`X-Forwarded-Proto` headers should be honoured.
* `Dpp__ForwardedHeaders__KnownNetworks__0`, `__1`, ...: Trusted proxy networks in CIDR form (e.g. `10.0.0.0/8`).
* `Dpp__ForwardedHeaders__TrustAllProxies`: Set to `true` **only** when the server is network-isolated behind a proxy and cannot be reached directly. (default: `false`)

> **On forwarded headers.** By default only loopback proxies are trusted, matching the framework default. If the server runs behind a reverse proxy and none of the settings above are configured, the per-IP rate limit will partition on the proxy's address (collapsing every caller into one bucket) and redirects may be built from the wrong scheme. Configure the proxy's address or network rather than `TrustAllProxies` wherever possible: trusting all proxies means any caller who can reach the server directly may spoof their client IP to evade rate limiting and spoof the scheme used for redirects.

**Note: A double underscore ('__') in environment variable keys creates nested configuration sections (hierarchical keys).**

## Deployment

Docker containers are automatically built for the UA Cloud Library. The latest version is always available via:

`docker pull ghcr.io/opcfoundation/ua-cloudlibrary:latest`

## Security – STRIDE Threat Analysis (UA-CloudLibrary server)

The following STRIDE-based threat model covers the `UACloudLibraryServer` project (the ASP.NET Core / Blazor Server application that exposes the REST API, Swagger UI, user-management UI, OPC UA Information Model upload/download, DPP service and the embedded OPC UA server). Each row identifies a representative threat for one of the six STRIDE categories and lists the corresponding in-code or operational mitigation already implemented in this repository, plus any residual recommendations for operators.

| # | STRIDE category | Asset / entry point | Threat scenario | Mitigation in `UACloudLibraryServer` |
|---|-----------------|---------------------|-----------------|---------------------------------------|
| 1 | **S**poofing | REST API & Swagger UI (`Controllers/*`, `/swagger`) | An anonymous caller impersonates a valid user to upload, delete or approve nodesets. | All API controllers are protected with `[Authorize(Policy = "ApiPolicy")]` (see `UploadController`, `InfoModelController`, `ApprovalController`, `AccessController`, `BrowserController`, `ExplorerController`, `SubmodelApiController`, `DPPLifecycleApiController`). The `ApiPolicy` (configured in `Startup.ConfigureServices`) requires an authenticated principal supplied by `BasicAuthenticationHandler`, `SignedInUserAuthenticationHandler` or, when `APIKeyAuth` is configured, `ApiKeyAuthenticationHandler`. |
| 2 | **S**poofing | Interactive UI / Identity area (`Areas/Identity/Pages/Account/*`) | An attacker creates an account using a victim's email address or hijacks a session. | ASP.NET Core Identity is used with confirmed-account sign-in (`RequireConfirmedAccount = true` whenever `EmailSenderAPIKey` is configured), email confirmation flow (`ConfirmEmail`, `ConfirmEmailChange`), password reset confirmation, lockout (`Lockout.cshtml`) and optional Google reCAPTCHA (`CaptchaValidation`) on registration. External identity providers (Microsoft Account, Azure Entra ID via `Microsoft.Identity.Web`, OPC Foundation OAuth2) are wired through `AddAuthentication()` in `Startup` so federated MFA can be enforced at the IdP. |
| 3 | **S**poofing | Service-to-service callers using API keys | A leaked or guessed key is replayed against the API. | API keys are issued per user via `ApiKeyTokenProvider` (registered through Identity's token-provider pipeline) and validated by `UserService.ValidateApiKeyAsync` from `ApiKeyAuthenticationHandler`. Keys are bound to the issuing Identity user, can be revoked from `ManageApiKeys.cshtml`, are transmitted only via the dedicated `X-API-Key` header (declared as the Swagger `ApiKeyAuth` security scheme) and are only honoured when the operator has explicitly opted-in via the `APIKeyAuth` environment variable. |
| 4 | **T**ampering | Inbound nodeset / values / DPP file uploads (`UploadController`, `DPPLifecycleApiController`, `AssetAdministrationShellEnvironmentService`) | A caller submits a malformed or malicious file (XXE, oversized payload, executable disguised as XML/JSON) to corrupt the library or trigger code execution. | `UploadController.UploadNodeset` validates that `nodesetFile.ContentType == "text/xml"` and `values.ContentType == "text/json"`, rejects empty payloads, wraps file names in `FileInfo` for path-character validation and persists content as text. Nodeset XML is parsed through the OPC Foundation `Opc.Ua.Configuration` / `NodesetModelFactoryOpc` pipeline which uses safe XML readers. All metadata fields (title, license, copyright, description, URLs) are individually validated before reaching `CloudLibDataProvider.UploadNamespaceAndNodesetAsync`. Operators should additionally configure Kestrel/IIS request-body size limits and front the service with a WAF. |
| 5 | **T**ampering | Database persistence (`AppDbContext`, `CloudLibDataProvider`, `DbFileStorage`) | SQL injection or direct DB tampering modifies stored nodesets, users or roles. | EF Core (`Microsoft.EntityFrameworkCore` / Npgsql) is used everywhere – all queries are parameterised LINQ. Schema is managed exclusively through versioned EF Core migrations under `Migrations/`. PostgreSQL credentials are taken from the `ConnectionStrings__CloudLibraryPostgreSQL` environment variable and never hard-coded. Operators are expected to grant the application a least-privilege DB role and to keep PostgreSQL ≥ 11.20. |
| 6 | **T**ampering | Data-protection keys & cookies | An attacker who reads the key ring forges authentication cookies or anti-forgery tokens. | `Startup.ConfigureServices` calls `services.AddDataProtection().PersistKeysToFileSystem(...).SetApplicationName(...)`, with the key-ring directory configurable via `DATA_PROTECTION_KEY_PATH` so it can be placed on a volume whose permissions are restricted to the application identity. The default is the process working directory, which in a container is discarded on restart &mdash; see [Optional Settings - Data Protection](#optional-settings---data-protection). Keys are stored unencrypted at rest, so filesystem permissions are the control; operators wanting defence in depth should additionally protect the ring with a KMS or certificate (`ProtectKeysWith*`). External-login correlation cookies are pinned to `SameSite=Strict` and `CookieSecurePolicy.Always`, and the entire pipeline runs behind `app.UseHttpsRedirection()`. ASP.NET Core's automatic anti-forgery token validation is active for the Razor Pages / Blazor UI. |
| 7 | **R**epudiation | Administrative actions (approve / delete nodesets, manage users, issue API keys) and DPP access | A user denies performing a destructive action, or denies reading controlled data, because actions are not auditable. | All privileged endpoints sit behind authenticated identities (Identity user or federated principal) so every request is bound to a `User.Identity.Name`. The upload pipeline records the uploader's identity (`_database.UploadNamespaceAndNodesetAsync(User.Identity.Name, ...)`). Every DPP read and modify, and every role grant/revoke, is additionally written to the hash-chained audit log (`IDppAuditLog` / `DppAuditLog`) bound to the acting operator; mutations write an `Attempted` record before the change and the outcome after, and a request whose audit entry cannot be committed fails with `503` rather than completing unlogged. Configure `Dpp__Audit__HmacKey` to authenticate the chain &mdash; **without it the digests are unkeyed, so anyone able to write to the audit tables can recompute them and pass verification**. See [what the audit log does and does not prove](#access-control-audit-and-signing-en-18239--en-18246). Application logging is enabled via `services.AddLogging(builder => builder.AddConsole())` and emits structured logs that can be shipped to a central SIEM/Log Analytics workspace from the container host. |
| 8 | **R**epudiation | External OAuth callback (`/Account/ExternalLogin`, `OAuthEvents.OnCreatingTicket`) | A replayed or forged ticket is accepted as a legitimate sign-in. | The OAuth handler enforces correlation cookies (`CorrelationCookie.SameSite = Strict`, `SecurePolicy = Always`), uses HTTPS-only token endpoints, calls `EnsureSuccessStatusCode()` on the userinfo response, and stamps a `TicketCreated` token into the authentication properties so the time of issuance is preserved alongside the access token. |
| 9 | **I**nformation disclosure | Stored user secrets (passwords, API keys, external tokens) | DB compromise leaks credentials usable elsewhere. | Passwords are stored as PBKDF2 hashes by ASP.NET Core Identity (`AddDefaultIdentity<IdentityUser>`). API keys are issued through `ApiKeyTokenProvider` (an Identity `IUserTwoFactorTokenProvider`) and validated server-side by `UserService.ValidateApiKeyAsync`; they are not echoed back to the user after creation. OAuth refresh/access tokens stored via `SaveTokens = true` are protected by ASP.NET Core Data Protection. |
| 10 | **I**nformation disclosure | Configuration / secrets surface | Secrets such as `ServicePassword`, `EmailSenderAPIKey`, `OAuth2ClientSecret`, `Authentication:Microsoft:ClientSecret`, `CaptchaSettings__SecretKey` and the PostgreSQL password leak via source control or logs. | All secrets are read from `IConfiguration` (environment variables / mounted secret stores) and are never committed to the repository. The README explicitly documents the env-var contract (`ServicePassword`, `EmailSenderAPIKey`, `Authentication:Microsoft:ClientSecret`, `OAuth2ClientSecret`, `CaptchaSettings__SecretKey`, `ConnectionStrings__CloudLibraryPostgreSQL`). The development-only exception page is gated by `env.IsDevelopment()` so stack traces are not returned in production. |
| 11 | **I**nformation disclosure | Network traffic to/from the server | Credentials, cookies or API keys captured on the wire. | `app.UseHttpsRedirection()` forces TLS for every request. External login cookies are marked `Secure`. Containers are expected to be fronted by a TLS-terminating reverse proxy / load balancer. The embedded OPC UA `SimpleServer` (`UAClientServer/SimpleServer.cs`) uses the standard `Opc.Ua.Configuration.ApplicationInstance` certificate store so that the `opc.tcp` channel is signed and encrypted. |
| 12 | **D**enial of service | Public registration / login / password-reset endpoints | Bots flood self-registration, exhaust the email quota, or brute-force passwords. | Self-registration can be disabled entirely via `AllowSelfRegistration=false`. Google reCAPTCHA v3 is enforced through `CaptchaValidation` (configurable score via `CaptchaSettings__BotThreshold`) on registration. Identity's built-in lockout (`Lockout.cshtml`) blocks password brute-force. Email sending is delegated to Postmark or SendGrid (`PostmarkEmailSender`, `SendGridEmailSender`) which apply provider-side rate limits. |
| 13 | **D**enial of service | Large or malicious uploads, expensive nodeset parsing | A caller uploads many huge nodesets to fill storage or pin CPU. | Upload endpoints require an authenticated identity (`ApiPolicy`) so anonymous flooding is not possible. Uploaded nodesets are streamed through `MemoryStream` and then handed to `CloudLibDataProvider.UploadNamespaceAndNodesetAsync` which deduplicates by deterministic hash (`DeterministicHash.cs`) and stores them in PostgreSQL via `DbFileStorage`. Operators should additionally configure Kestrel (`KestrelServerOptions`) request-body size limits and HTTP timeouts at the reverse proxy. |
| 14 | **D**enial of service | Embedded OPC UA server (`UAClientServer/SimpleServer.cs`, `NodesetFileNodeManager.cs`) | A malicious OPC UA client opens excessive sessions/subscriptions or sends malformed messages. | The server is built on the OPC Foundation `Opc.Ua.Server` stack which enforces session limits, message size limits and security-policy validation through the configured `ApplicationInstance`. The OPC UA application certificate is created and validated automatically by `ApplicationInstance` so unsigned channels are rejected. |
| 15 | **D**enial of service | Anonymous DPP read endpoints (`DPPLifecycleApiController`, `v1/dpps/*`) | EN 18246 requires public DPP data to be readable without login, so an unauthenticated caller can scrape or flood these routes. | The DPP endpoints carry `[EnableRateLimiting(Startup.DppRateLimitPolicy)]`, a fixed-window limiter partitioned per client IP and returning `429` when exceeded; the per-minute allowance is configurable via `Dpp__RateLimit__PermitPerMinute` (default 100). Partitioning depends on seeing the real client address, so behind a proxy the `Dpp__ForwardedHeaders__*` settings must be configured &mdash; otherwise every caller shares the proxy's bucket. Each read also issues an ESDC (an RSA signature), so the limiter bounds signing cost as well as data egress. |
| 16 | **E**levation of privilege | Administrative endpoints (approval, user/role management) | A regular user escalates to administrator and approves or deletes arbitrary nodesets, or an administrator is locked out so nobody can administer the system. | Administrative operations are protected by the `AdministrationPolicy` defined in `Startup.ConfigureServices` (`policy.RequireRole(Roles.Administrator)`, value `"Administrator"`). The role can only be assigned by an existing administrator via `AccessController`, and the bootstrap admin credentials are supplied out-of-band via the `ServicePassword` (and optional `ServiceUsername`, default `admin`) environment variables. API-key principals only carry the claims of the user that minted them, so a compromised key cannot exceed that user's role set. The role itself is also protected against self-inflicted lockout: deleting the `Administrator` role, or revoking it from the last remaining administrator, is refused with `403`, because neither is recoverable in-band once the accounts able to repair it have lost access. The last-administrator check runs under a PostgreSQL advisory lock so two concurrent revocations cannot both observe a safe count and leave none. |
| 17 | **E**levation of privilege | Authentication-handler bypass | A bug in a custom authentication handler grants access without valid credentials. | The custom handlers (`BasicAuthenticationHandler`, `SignedInUserAuthenticationHandler`, `ApiKeyAuthenticationHandler`) all delegate credential verification to `UserService` which uses the Identity `UserManager`/`SignInManager` APIs (PBKDF2 password verification, normalised user lookup, time-constant comparisons). Authentication failures consistently return `AuthenticateResult.Fail/NoResult` and never short-circuit the pipeline as success. The combined `ApiPolicy` requires `RequireAuthenticatedUser()` so a `NoResult` from one scheme cannot be interpreted as success. |
| 18 | **I**nformation disclosure | Role-controlled DPP data elements (`DPPService`, `DppControlledElements`) | EN 18246 requires different economic operators to see different subsets of the same passport, so a caller reads elements their role is not entitled to. | Every DPP leaving the service is passed through `DPPService.FilterForRolesAsync`, which drops elements whose `controlledElements` entry does not grant the caller's roles; the filter is applied on the read path itself rather than in the UI, so REST, GraphQL and browse responses are filtered identically. `PATCH` responses are re-filtered after the write so a mutation cannot echo back data the caller could not have read. Writes are separately gated by `CanWriteElementAsync`, and nodeset-level visibility is enforced by `IsNodesetAccessibleAsync` before element filtering runs. Elements with no `controlledElements` entry are treated as public by design &mdash; access is deny-by-default only for elements explicitly placed under control. |
| 19 | **S**poofing | ESDC verification / trust anchors (`RsaEsdcService`) | A peer presents a self-signed ESDC claiming to originate from another economic operator, and the server accepts it as that operator's attestation. | ESDCs are W3C VC-JWT signed with RS256. Trust anchors are configured issuer-bound via `Dpp__Esdc__TrustedIssuers__N__{Issuer,PublicKeyPem,KeyId}`, and `AnchorMayVouchFor` requires the anchor to match the **signed** `issuer` claim (and the `kid`, when pinned) before the signature is accepted &mdash; so holding a trusted key does not let an operator sign for a different issuer. The issuer and `validFrom` are read from the signed payload, never from the unprotected header. Where a certificate is supplied, `EnsureCertificateMatchesSigningKey` verifies it binds to the signing key. The legacy unbound `Dpp__Esdc__TrustedPublicKeysPem` list is still honoured for compatibility but **any key in it can vouch for any issuer**; prefer the issuer-bound form. |

### API Key Security Features

The UA Cloud Library implements comprehensive security measures for API key authentication to protect against various attack vectors:

#### **DOS Attack Prevention**
* **Rate Limiting:** A mandatory 150ms delay is applied to every API key validation attempt, effectively limiting attackers to **~6-7 validation attempts per second** per connection
* **Resource Protection:** Prevents rapid-fire requests from overwhelming the server
* **CPU/Database Protection:** Reduces the load from brute-force attempts on password hashing and database queries

#### **Brute-Force Attack Mitigation**
* **Time Cost:** The 150ms validation delay makes brute-force attacks **~150x slower** (from ~1000s of attempts/sec to ~6-7 attempts/sec)
* **Practical Impact:** To test 1 million API keys would require:
  - **Without delay:** ~16 minutes (at 1000/sec)
  - **With delay:** ~1.7 days (at 6.67/sec)
* **Exponential Deterrent:** Combined with account lockouts, makes attacks practically infeasible

#### **API Key Type and Expiration**
* **Access Control:** API keys can be configured as **Read-Only** or **Read-Write** to limit permissions
  - **Read-Write Keys:** Required for all mutating operations (POST, PUT, DELETE)
  - **Read-Only Keys:** Only permitted for read operations (GET); automatically rejected for POST/PUT/DELETE endpoints
  - **Automatic Enforcement:** Authorization policy enforces access restrictions at the HTTP method level
* **Automatic Expiration:** Keys can be set to expire after configurable periods:
  - 1 Day
  - 30 Days
  - 6 Months
  - 1 Year
  - Unlimited (no expiration)
* **Expiration Enforcement:** Expired keys are automatically rejected during validation
* **Audit Trail:** Failed attempts with expired keys are logged for security monitoring

#### **Client Library Support**

The `Opc.Ua.CloudLib.Client` NuGet package provides native support for API key authentication:

```csharp
// Simple API key usage
var client = new UACloudLibClient("CLxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxx");

// With custom endpoint
var client = new UACloudLibClient(
    "https://uacloudlibrary.opcfoundation.org",
    "CLxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxx"
);

// Uploading requires Read-Write API key
var (status, message) = await client.UploadNodeSetAsync(myNodeset);
```

**Best Practices for API Key Usage:**
- Use Read-Only keys for applications that only browse or download nodesets
- Use Read-Write keys only for applications that need to upload or modify content
- Store API keys in environment variables or secure vaults, never in source code
- Set appropriate expiration dates based on your security requirements
- Rotate keys regularly and delete unused keys

For complete client library documentation and examples, see the [Client Library README](Opc.Ua.CloudLib.Client/README.md).

#### **Cryptographic Security**
* **Secure Generation:** API keys are generated using `RandomNumberGenerator.GetBytes(32)` (256-bit entropy)
* **Base64URL Encoding:** Keys are encoded using Base64URL to ensure safe transmission in HTTP headers
* **Password Hashing:** Keys are hashed using ASP.NET Core Identity's PBKDF2 implementation before storage
* **Prefix Storage:** Only the first 4 characters are stored unhashed for efficient lookup while maintaining security

#### **Metadata and Auditing**
* **Metadata Format:** API key metadata is stored alongside the hash: `{prefix}{hash}|Type:{type}|Expiration:{period}|ExpiresAt:{ISO8601-date}`
* **Audit Logging:** All validation failures, expired key usage, and cache collisions are logged with warnings
* **Timing Attack Prevention:** Fixed 150ms delay is applied regardless of validation outcome (success, failure, cache hit, or cache miss)

#### **Attack Scenario Effectiveness**

| Attack Type | Without Delay | With 150ms Delay | Effectiveness |
|-------------|---------------|------------------|---------------|
| Brute Force (1M keys) | 16 minutes | 1.7 days | **~99% slower** ✅ |
| DOS (1000 req/sec) | Server overload | Max ~6-7 req/sec | **~99.3% reduction** ✅ |
| Timing Analysis | Exploitable | Fixed timing | **Mitigated** ✅ |

#### **Performance Considerations**
* **Async Implementation:** Uses `Task.Delay()` which doesn't block threads, allowing the server to handle other requests during the delay
* **Non-Blocking:** Better than thread-blocking alternatives like `Thread.Sleep()`
* **Cache-Aware:** Even cached keys experience the validation delay, maintaining consistent security
* **Acceptable Overhead:** For REST API calls, 150ms is typically acceptable latency

### Residual recommendations for operators

* Run the container behind a TLS-terminating reverse proxy or ingress controller, and configure HSTS at that layer.
* Mount the Data-Protection key directory on a persisted, access-controlled volume (or use Azure Key Vault / Blob storage providers) so cookie keys survive restarts but are not world-readable.
* Restrict the PostgreSQL account to the `uacloudlib` database and avoid using a superuser account for the application connection string.
* Enable Google reCAPTCHA (`CaptchaSettings__Enabled=true`) and a strong `ServicePassword` in any production deployment.
* Forward console logs to a central SIEM (e.g. Azure Monitor / Log Analytics) to support audit and repudiation investigations.
* Keep dependencies (ASP.NET Core, Npgsql, OPC UA stack, identity providers) on the latest patched versions via the existing GitHub Actions pipelines.

