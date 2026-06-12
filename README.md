# UA Cloud Library

The reference implementation of the UA Cloud Library. The UA Cloud Library enables the storage in and querying of OPC UA Information Models from anywhere in the world. 

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

If you want to access your own instance or the globally hosted instance from the OPC Foundation at https://uacloudlibrary.opcfoundation.org from our software, you can integrate the source code from the SampleConsoleClient found in this repo. It exercises the REST API.

**Warning:** In the latest version of the REST API, a new infomodel/find2 API is introduced, returning a [UANameSpace](https://raw.githubusercontent.com/OPCFoundation/UA-CloudLibrary/refs/heads/main/Opc.Ua.CloudLib.Client/Models/UANameSpace.cs) structure, to align it with the rest of the REST API. The SampleConsoleClient is updated to work with the latest version of the REST API. Please update your client code accordingly if you were using an older version of the REST API! The older version will be removed in a future version of the API.

## Development Setup

Start development in three simple steps:

1. Checkout ``git clone https://github.com/OPCFoundation/UA-CloudLibrary.git``
2. Open with Visual Studio 2019+
3. Select ``docker-compose`` as startup project and hit F5 or the "play button"

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

* **EN 18221** &mdash; *Digital product passport - data storage, archiving, and data persistence* &mdash; shapes the storage, archiving and version-retrieval behaviour (Clause 4.1 storage, Clause 4.2 archiving).
* **EN 18222** &mdash; *Digital Product Passport - Application Programming Interfaces (APIs) for the product passport lifecycle management and searchability* &mdash; shapes the REST surface (`ReadDppById`, `ReadDppByProductId`, `ReadDppIdsByProductIds`, `ReadDataElement`, `UpdateDppById`, `UpdateDataElement`, `ReadDppVersionByIdAndDate`).
* **EN 18223** &mdash; *Digital Product Passport - System interoperability* &mdash; shapes the semantic data model (Clause 4: `DigitalProductPassport` and its `DataElement` subclasses) and the JSON serialization (Clause 5 / Annex A).

The DPP surface is implemented by:

* [`Controllers/DPPLifecycleApiController.cs`](UACloudLibraryServer/Controllers/DPPLifecycleApiController.cs) &mdash; the HTTP boundary.
* [`DPPService.cs`](UACloudLibraryServer/DPPService.cs) &mdash; the service-layer orchestration.
* [`UAClientServer/UAClient.cs`](UACloudLibraryServer/UAClientServer/UAClient.cs) &mdash; live access to the embedded OPC UA server.
* [`DbFileStorage.cs`](UACloudLibraryServer/DbFileStorage.cs) &mdash; durable persistence of nodeset XML and variable values.
* [`IDppVersionArchive.cs`](UACloudLibraryServer/IDppVersionArchive.cs) and [`DbFileVersionArchive.cs`](UACloudLibraryServer/DbFileVersionArchive.cs) &mdash; durable archive of DPP version snapshots.
* [`Models/DppModel.cs`](UACloudLibraryServer/Models/DppModel.cs), [`Models/DppApiResponse.cs`](UACloudLibraryServer/Models/DppApiResponse.cs), [`Models/Pagination.cs`](UACloudLibraryServer/Models/Pagination.cs) &mdash; request/response contracts.
* [`DppJsonPath.cs`](UACloudLibraryServer/DppJsonPath.cs) &mdash; the JSONPath subset used for `elementIdPath` addressing.

### Conceptual model

A DPP is constructed on-demand from an OPC UA nodeset that has been uploaded to the Cloud Library. The DPP root, its `uniqueProductIdentifier`, `granularity`, `dppStatus`, `lastUpdate` timestamp, `economicOperatorId`/`facilityId`, `contentSpecificationIds`, and the tree of `DataElement` children are read from the live OPC UA address space rooted at the `Objects` folder of the addressed nodeset. The DPP identifier (`dppId`) is the nodeset identifier issued by the Cloud Library on upload.

The DPP header follows EN 18223 Clause 4.1.2.1 Table 1:

| Property | Cardinality | Notes |
|---|---|---|
| `digitalProductPassportId` | [1] | Globally unique, URI/URL-shaped. |
| `uniqueProductIdentifier` | [1] | Product identifier per EN 18219. |
| `granularity` | [1] | Enumeration: `Model`, `Batch`, `Item`. |
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
  "digitalProductPassportId": "https://uacloudlibrary.example.org/dpp/123",
  "uniqueProductIdentifier": "https://example.org/products/abc",
  "granularity": "Model",
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

All routes are versioned under `v1/` and require an authenticated principal satisfying the `ApiPolicy` (Basic auth, signed-in cookie, or `X-API-Key` &mdash; see [Authentication and Authorization](#authentication-and-authorization)).

| Method | Route | DPP lifecycle operation |
|---|---|---|
| `GET`   | `v1/dpps/{dppId}` | Returns the full DPP rooted at the addressed nodeset. |
| `GET`   | `v1/dppsByProductId/{productId}` | Returns the latest DPP whose `UniqueProductIdentifier` equals `productId` (resolved by browsing all nodesets visible to the caller and picking the newest `lastUpdate`). |
| `POST`  | `v1/dppsByProductIds` | Returns the list of DPP identifiers matching the supplied `productIds`. Supports paging via `?limit=` and `?cursor=` query parameters; the response envelope carries a `pagination` block. |
| `GET`   | `v1/dpps/{dppId}/elements/{*elementIdPath}` | Returns the `DataElement` addressed by the JSONPath subset described below. |
| `PATCH` | `v1/dpps/{dppId}` | Applies a partial DPP using RFC 7396 JSON Merge Patch semantics. Accepts `application/json` or `application/merge-patch+json`. Snapshots the pre-update DPP into the archive, writes leaf values to the live OPC UA server, then persists the new values. |
| `PATCH` | `v1/dpps/{dppId}/elements/{*elementIdPath}` | Updates a single addressed leaf element. Snapshots, writes, and persists as above. |
| `GET`   | `v1/dpps/{dppId}/versions/{date}` | Returns the DPP snapshot that was active at the supplied ISO 8601 timestamp. If `date` is &ge; "now", the live DPP is returned; otherwise the archive is consulted. Returns `404` when no version existed at that point in time. |

The request body for `POST v1/dppsByProductIds` is:

```json
{ "productIds": ["urn:product:1", "urn:product:2"] }
```

### `elementIdPath` &mdash; JSONPath addressing

`elementIdPath` is a JSONPath expression rooted at the `DigitalProductPassport.elements` collection. The parser ([`DppJsonPath`](UACloudLibraryServer/DppJsonPath.cs)) accepts the subset of RFC 9535 actually used by the DPP data model:

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

1. **Pre-update snapshot.** Before any write, `DPPService` browses the current DPP and calls `IDppVersionArchive.ArchiveAsync(dppId, snapshot, DateTimeOffset.UtcNow)`. This satisfies the EN 18221 Clause 4.2 requirement that *“archiving starts when the first change of the initial digital product passport occurs”* and that *“all changes to the digital product passport shall be archived”*.
2. **Live write.** The resolved leaf values are written to the running embedded OPC UA server via `UAClient.VariableWrite(...)`. `UAClient` does *not* persist anything to the database.
3. **Explicit persistence.** After the live write succeeds, `DPPService.PersistNodesetValuesAsync(...)` re-browses the variables and upserts the serialized values into `DbFiles.Values` through `DbFileStorage.UploadFileAsync(...)`, so the change survives a server restart (the embedded OPC UA server rehydrates values from `DbFiles.Values` on startup via `NodesetFileNodeManager.AddNodesAndValues`).

To prevent a save from silently overwriting an earlier on-disk version, the persistence step rewrites the `PublicationDate="..."` attribute in the stored nodeset XML to the current UTC timestamp (second precision, `yyyy-MM-ddTHH:mm:ssZ`). This mirrors the in-XML date bump already used by `UAClient.CopyNodeset` and ensures every persisted save is uniquely datable.

### Durable version archive

The archive implements the archiving rules of EN 18221 Clause 4.2 (point-in-time retrievability of all past changes during the DPP lifetime) by reusing the existing `DbFileStorage`:

* Each snapshot is stored as its own row in the `DbFiles` table whose `Name` follows the layout `dpp-archive::{dppId}::{capturedAtUtcTicks:D19}` and whose `Blob` holds the JSON-serialized `DigitalProductPassport`.
* The fixed-width 19-digit tick suffix makes lexicographic ordering match chronological order, so retrieving the snapshot at or before a target timestamp is an ordered prefix scan via `DbFileStorage.ListFileNamesAsync(prefix)`.
* If two snapshots collide on the same UTC tick (concurrent updates), the row name is nudged forward by one tick at a time until a free key is found, so older snapshots are never overwritten.

`DbFileVersionArchive` is registered as a scoped service in `Startup.ConfigureServices`, aligned with the scoped `AppDbContext` that backs `DbFileStorage`.

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
You **must** have installed PostgreSQL version 11.20 or higher. You **must** also define one of the following two sets of environment variables:

#### PostgreSQL Set 1: Three environment variables
* `PostgreSQLEndpoint`: The endpoint of the PostgreSQL instance (that must be previously deployed in the hosting platform).
* `PostgreSQLUsername`: The username to use to log in to the PostgreSQL instance.
* `PostgreSQLPassword`: The password to use to log in to the PostgreSQL instance.

#### PostgreSQL Set 2: One connection string
*  `ConnectionStrings__CloudLibraryPostgreSQL`: All of the above values, as a connection string instead of as individual environment variables. Example:
```
"Server=localhost; Username=MyUserName;Password=MyUserPassword;Database=uacloudlib;Port=5432;Include Error Detail=true",
```
**Note: that you must create a user account with set privileges to access the database.**

### Setting Password for Admin Account
To enable access, from both Swagger and the REST API, you must set a password using this environment variable:
* `ServicePassword`: The administration password for Swagger and REST service.

**Note: The user name is `admin`.**

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
| 5 | **T**ampering | Database persistence (`AppDbContext`, `CloudLibDataProvider`, `DbFileStorage`) | SQL injection or direct DB tampering modifies stored nodesets, users or roles. | EF Core (`Microsoft.EntityFrameworkCore` / Npgsql) is used everywhere – all queries are parameterised LINQ. Schema is managed exclusively through versioned EF Core migrations under `Migrations/`. PostgreSQL credentials are taken from environment variables (`PostgreSQLEndpoint/Username/Password` or `ConnectionStrings__CloudLibraryPostgreSQL`) and never hard-coded. Operators are expected to grant the application a least-privilege DB role and to keep PostgreSQL ≥ 11.20. |
| 6 | **T**ampering | Data-protection keys & cookies | An attacker who reads the key ring forges authentication cookies or anti-forgery tokens. | `Startup.ConfigureServices` calls `services.AddDataProtection().PersistKeysToFileSystem(...)` so keys are persisted (and can be mounted on a protected volume in container deployments). External-login correlation cookies are pinned to `SameSite=Strict` and `CookieSecurePolicy.Always`, and the entire pipeline runs behind `app.UseHttpsRedirection()`. ASP.NET Core's automatic anti-forgery token validation is active for the Razor Pages / Blazor UI. |
| 7 | **R**epudiation | Administrative actions (approve / delete nodesets, manage users, issue API keys) | A user denies performing a destructive action because actions are not auditable. | All privileged endpoints sit behind authenticated identities (Identity user or federated principal) so every request is bound to a `User.Identity.Name`. The upload pipeline records the uploader's identity (`_database.UploadNamespaceAndNodesetAsync(User.Identity.Name, ...)`). Application logging is enabled via `services.AddLogging(builder => builder.AddConsole())` and emits structured logs that can be shipped to a central SIEM/Log Analytics workspace from the container host. |
| 8 | **R**epudiation | External OAuth callback (`/Account/ExternalLogin`, `OAuthEvents.OnCreatingTicket`) | A replayed or forged ticket is accepted as a legitimate sign-in. | The OAuth handler enforces correlation cookies (`CorrelationCookie.SameSite = Strict`, `SecurePolicy = Always`), uses HTTPS-only token endpoints, calls `EnsureSuccessStatusCode()` on the userinfo response, and stamps a `TicketCreated` token into the authentication properties so the time of issuance is preserved alongside the access token. |
| 9 | **I**nformation disclosure | Stored user secrets (passwords, API keys, external tokens) | DB compromise leaks credentials usable elsewhere. | Passwords are stored as PBKDF2 hashes by ASP.NET Core Identity (`AddDefaultIdentity<IdentityUser>`). API keys are issued through `ApiKeyTokenProvider` (an Identity `IUserTwoFactorTokenProvider`) and validated server-side by `UserService.ValidateApiKeyAsync`; they are not echoed back to the user after creation. OAuth refresh/access tokens stored via `SaveTokens = true` are protected by ASP.NET Core Data Protection. |
| 10 | **I**nformation disclosure | Configuration / secrets surface | Secrets such as `ServicePassword`, `EmailSenderAPIKey`, `OAuth2ClientSecret`, `Authentication:Microsoft:ClientSecret`, `CaptchaSettings__SecretKey` and the PostgreSQL password leak via source control or logs. | All secrets are read from `IConfiguration` (environment variables / mounted secret stores) and are never committed to the repository. The README explicitly documents the env-var contract (`ServicePassword`, `EmailSenderAPIKey`, `Authentication:Microsoft:ClientSecret`, `OAuth2ClientSecret`, `CaptchaSettings__SecretKey`, `PostgreSQLPassword`, `ConnectionStrings__CloudLibraryPostgreSQL`). The development-only exception page is gated by `env.IsDevelopment()` so stack traces are not returned in production. |
| 11 | **I**nformation disclosure | Network traffic to/from the server | Credentials, cookies or API keys captured on the wire. | `app.UseHttpsRedirection()` forces TLS for every request. External login cookies are marked `Secure`. Containers are expected to be fronted by a TLS-terminating reverse proxy / load balancer. The embedded OPC UA `SimpleServer` (`UAClientServer/SimpleServer.cs`) uses the standard `Opc.Ua.Configuration.ApplicationInstance` certificate store so that the `opc.tcp` channel is signed and encrypted. |
| 12 | **D**enial of service | Public registration / login / password-reset endpoints | Bots flood self-registration, exhaust the email quota, or brute-force passwords. | Self-registration can be disabled entirely via `AllowSelfRegistration=false`. Google reCAPTCHA v3 is enforced through `CaptchaValidation` (configurable score via `CaptchaSettings__BotThreshold`) on registration. Identity's built-in lockout (`Lockout.cshtml`) blocks password brute-force. Email sending is delegated to Postmark or SendGrid (`PostmarkEmailSender`, `SendGridEmailSender`) which apply provider-side rate limits. |
| 13 | **D**enial of service | Large or malicious uploads, expensive nodeset parsing | A caller uploads many huge nodesets to fill storage or pin CPU. | Upload endpoints require an authenticated identity (`ApiPolicy`) so anonymous flooding is not possible. Uploaded nodesets are streamed through `MemoryStream` and then handed to `CloudLibDataProvider.UploadNamespaceAndNodesetAsync` which deduplicates by deterministic hash (`DeterministicHash.cs`) and stores them in PostgreSQL via `DbFileStorage`. Operators should additionally configure Kestrel (`KestrelServerOptions`) request-body size limits and HTTP timeouts at the reverse proxy. |
| 14 | **D**enial of service | Embedded OPC UA server (`UAClientServer/SimpleServer.cs`, `NodesetFileNodeManager.cs`) | A malicious OPC UA client opens excessive sessions/subscriptions or sends malformed messages. | The server is built on the OPC Foundation `Opc.Ua.Server` stack which enforces session limits, message size limits and security-policy validation through the configured `ApplicationInstance`. The OPC UA application certificate is created and validated automatically by `ApplicationInstance` so unsigned channels are rejected. |
| 15 | **E**levation of privilege | Administrative endpoints (approval, user/role management) | A regular user escalates to administrator and approves or deletes arbitrary nodesets. | Administrative operations are protected by the `AdministrationPolicy` defined in `Startup.ConfigureServices` (`policy.RequireRole("Administrator")`). The `Administrator` role can only be assigned by an existing administrator via the management UI, and the bootstrap admin password is supplied out-of-band via the `ServicePassword` environment variable (the user name is fixed to `admin`). API-key principals only carry the claims of the user that minted them, so a compromised key cannot exceed that user's role set. |
| 16 | **E**levation of privilege | Authentication-handler bypass | A bug in a custom authentication handler grants access without valid credentials. | The custom handlers (`BasicAuthenticationHandler`, `SignedInUserAuthenticationHandler`, `ApiKeyAuthenticationHandler`) all delegate credential verification to `UserService` which uses the Identity `UserManager`/`SignInManager` APIs (PBKDF2 password verification, normalised user lookup, time-constant comparisons). Authentication failures consistently return `AuthenticateResult.Fail/NoResult` and never short-circuit the pipeline as success. The combined `ApiPolicy` requires `RequireAuthenticatedUser()` so a `NoResult` from one scheme cannot be interpreted as success. |

### Residual recommendations for operators

* Run the container behind a TLS-terminating reverse proxy or ingress controller, and configure HSTS at that layer.
* Mount the Data-Protection key directory on a persisted, access-controlled volume (or use Azure Key Vault / Blob storage providers) so cookie keys survive restarts but are not world-readable.
* Restrict the PostgreSQL account to the `uacloudlib` database and avoid using a superuser account for the application connection string.
* Enable Google reCAPTCHA (`CaptchaSettings__Enabled=true`) and a strong `ServicePassword` in any production deployment.
* Forward console logs to a central SIEM (e.g. Azure Monitor / Log Analytics) to support audit and repudiation investigations.
* Keep dependencies (ASP.NET Core, Npgsql, OPC UA stack, identity providers) on the latest patched versions via the existing GitHub Actions pipelines.

## Build Status

[![Docker Image CI](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml)

[![.NET](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml)
