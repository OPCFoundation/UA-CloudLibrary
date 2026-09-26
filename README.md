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
- [Database Configuration](#database-configuration)
- [Cloud Hosting Setup](#cloud-hosting-setup)
  - [Required environment variables](#required-environment-variables)
  - [Migrating from version 1.0 to version 1.1](#migrating-from-version-10-to-version-11)
  - [Required Settings - PostgreSQL](#required-settings---postgresql)
  - [Setting Credentials for Admin Account](#setting-credentials-for-admin-account)
  - [Optional Settings](#optional-settings)
  - [Optional Settings - Captcha](#optional-settings---captcha)
  - [Optional Settings - Data Protection](#optional-settings---data-protection)
  - [Settings - DPP](#settings---dpp)
- [Deployment](#deployment)
- [Security - STRIDE Threat Analysis (UA-CloudLibrary server)](#security---stride-threat-analysis-ua-cloudlibrary-server)
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

The UA Cloud Library can expose uploaded OPC UA information models as Digital Product Passports, aligned with EN 18221, EN 18222, EN 18223, EN 18239 and EN 18246.

The DPP Lifecycle API, its access-control model, audit log, signing and configuration are documented separately in **[dpp.md](dpp.md)**.

## Database Configuration
The UA Cloud Library database configuration is documented in the [Database Setup](Docs/Database%20Setup.md) document.

## Cloud Hosting Setup

### Required environment variables

Set these before deploying. The first three cause the server to **fail fast at startup** if missing; `ServicePassword` instead blocks admin login, which is easy to mistake for a wrong password.

| Variable | Required | If missing |
|---|---|---|
| `ConnectionStrings__CloudLibraryPostgreSQL` | Always | Server fails to start. Npgsql connection string to the PostgreSQL instance &mdash; see [Required Settings - PostgreSQL](#required-settings---postgresql). |
| `Dpp__Esdc__PrivateKeyPem` | Outside Development | Server fails to start. PKCS#8 PEM key used to sign Digital Product Passport credentials; supply it from a managed secret store. Set `Dpp__Esdc__AllowGeneratedSigningKey=true` instead to accept a server-generated key held in the database &mdash; intended for development and evaluation only. See [dpp.md](dpp.md#signing-key-management). |
| `Dpp__Esdc__EconomicOperatorId` | Whenever a signing key is resolved | Server fails to start. The economic operator this deployment signs for; without it the server would sign credentials asserting whichever operator a hosted passport happens to name. See [dpp.md](dpp.md#issuer-binding). |
| `ServicePassword` | Always | Server starts, but **admin login always fails** &mdash; the rejection looks like a wrong password. Administration password for Swagger and the REST API; see [Setting Credentials for Admin Account](#setting-credentials-for-admin-account). |

> **Upgrading an existing deployment?** The two `Dpp__Esdc__*` variables are newer requirements. A deployment that starts cleanly today may refuse to start after upgrading until both are set.

Everything else is optional and documented below.

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
* `DATA_PROTECTION_APPLICATION_NAME`: Overrides the Data Protection application discriminator. **Leave unset unless you need it** &mdash; see the warning below. (default: derived by the framework from the content root path)

> **Set `DATA_PROTECTION_KEY_PATH` in any containerised deployment.** The key ring encrypts authentication cookies, antiforgery tokens and the e-mail confirmation / password-reset tokens, so it has to outlive the process. The default is the container's working directory, which is discarded on every restart &mdash; losing the keys signs every user out and makes outstanding reset links fail. The failure is also misleading rather than obvious: the antiforgery token on an already-open login page can no longer be validated, so the POST is rejected before the credentials are ever checked, which presents to the user as a wrong password. Point this at a mounted volume that survives restarts, and share it across replicas so a request handled by one instance can be decrypted by another.

> **Changing `DATA_PROTECTION_APPLICATION_NAME` invalidates existing cookies and tokens.** The application name is an input to key derivation, so payloads protected under one discriminator cannot be read under another &mdash; even with the identical key ring. Setting, changing or removing this value therefore signs every user out and breaks outstanding password-reset and e-mail-confirmation links, exactly as if the keys had been lost. The framework default is derived from the content root path, which is already stable across restarts and identical across replicas of the same image, so most deployments should leave this unset. Set it only when replicas must share a key ring without sharing a content root path, and treat adopting it as a one-time breaking change to perform in a maintenance window.

### Settings - DPP

`Dpp__Esdc__PrivateKeyPem` and `Dpp__Esdc__EconomicOperatorId` are **required outside Development** &mdash; see [Required environment variables](#required-environment-variables) above.

The remaining DPP settings are optional: signing trust anchors, audit keys, rate limiting and reverse-proxy configuration are documented in [dpp.md](dpp.md#configuration-reference).

**Note: A double underscore ('__') in environment variable keys creates nested configuration sections (hierarchical keys).**

## Deployment

Docker containers are automatically built for the UA Cloud Library. The latest version is always available via:

`docker pull ghcr.io/opcfoundation/ua-cloudlibrary:latest`

## Security - STRIDE Threat Analysis (UA-CloudLibrary server)

The following STRIDE-based threat model covers the `UACloudLibraryServer` project (the ASP.NET Core / Blazor Server application that exposes the REST API, Swagger UI, user-management UI, OPC UA Information Model upload/download, DPP service and the embedded OPC UA server). Each row identifies a representative threat for one of the six STRIDE categories and lists the corresponding in-code or operational mitigation already implemented in this repository, plus any residual recommendations for operators.

| # | STRIDE category | Asset / entry point | Threat scenario | Mitigation in `UACloudLibraryServer` |
|---|-----------------|---------------------|-----------------|---------------------------------------|
| 1 | **S**poofing | REST API & Swagger UI (`Controllers/*`, `/swagger`) | An anonymous caller impersonates a valid user to upload, delete or approve nodesets. | All API controllers are protected with `[Authorize(Policy = "ApiPolicy")]` (see `UploadController`, `InfoModelController`, `ApprovalController`, `AccessController`, `BrowserController`, `ExplorerController`, `SubmodelApiController`, `DPPLifecycleApiController`). The `ApiPolicy` (configured in `Startup.ConfigureServices`) requires an authenticated principal supplied by `BasicAuthenticationHandler`, `SignedInUserAuthenticationHandler` or, when `APIKeyAuth` is configured, `ApiKeyAuthenticationHandler`. |
| 2 | **S**poofing | Interactive UI / Identity area (`Areas/Identity/Pages/Account/*`) | An attacker creates an account using a victim's email address or hijacks a session. | ASP.NET Core Identity is used with confirmed-account sign-in (`RequireConfirmedAccount = true` whenever `EmailSenderAPIKey` is configured), email confirmation flow (`ConfirmEmail`, `ConfirmEmailChange`), password reset confirmation, lockout (`Lockout.cshtml`) and optional Google reCAPTCHA (`CaptchaValidation`) on registration. External identity providers (Microsoft Account, Azure Entra ID via `Microsoft.Identity.Web`, OPC Foundation OAuth2) are wired through `AddAuthentication()` in `Startup` so federated MFA can be enforced at the IdP. |
| 3 | **S**poofing | Service-to-service callers using API keys | A leaked or guessed key is replayed against the API. | API keys are issued per user via `ApiKeyTokenProvider` (registered through Identity's token-provider pipeline) and validated by `UserService.ValidateApiKeyAsync` from `ApiKeyAuthenticationHandler`. Keys are bound to the issuing Identity user, can be revoked from `ManageApiKeys.cshtml`, are transmitted only via the dedicated `X-API-Key` header (declared as the Swagger `ApiKeyAuth` security scheme) and are only honoured when the operator has explicitly opted-in via the `APIKeyAuth` environment variable. |
| 4 | **T**ampering | Inbound nodeset / values / DPP file uploads (`UploadController`, `DPPLifecycleApiController`, `AssetAdministrationShellEnvironmentService`) | A caller submits a malformed or malicious file (XXE, oversized payload, executable disguised as XML/JSON) to corrupt the library or trigger code execution. | `UploadController.UploadNodeset` validates that `nodesetFile.ContentType == "text/xml"` and `values.ContentType == "text/json"`, rejects empty payloads, wraps file names in `FileInfo` for path-character validation and persists content as text. Nodeset XML is parsed through the OPC Foundation `Opc.Ua.Configuration` / `NodesetModelFactoryOpc` pipeline which uses safe XML readers. All metadata fields (title, license, copyright, description, URLs) are individually validated before reaching `CloudLibDataProvider.UploadNamespaceAndNodesetAsync`. Operators should additionally configure Kestrel/IIS request-body size limits and front the service with a WAF. |
| 5 | **T**ampering | Database persistence (`AppDbContext`, `CloudLibDataProvider`, `DbFileStorage`) | SQL injection or direct DB tampering modifies stored nodesets, users or roles. | EF Core (`Microsoft.EntityFrameworkCore` / Npgsql) is used everywhere -œ all queries are parameterised LINQ. Schema is managed exclusively through versioned EF Core migrations under `Migrations/`. PostgreSQL credentials are taken from the `ConnectionStrings__CloudLibraryPostgreSQL` environment variable and never hard-coded. Operators are expected to grant the application a least-privilege DB role and to keep PostgreSQL  11.20. |
| 6 | **T**ampering | Data-protection keys & cookies | An attacker who reads the key ring forges authentication cookies or anti-forgery tokens. | `Startup.ConfigureServices` calls `services.AddDataProtection().PersistKeysToFileSystem(...).SetApplicationName(...)`, with the key-ring directory configurable via `DATA_PROTECTION_KEY_PATH` so it can be placed on a volume whose permissions are restricted to the application identity. The default is the process working directory, which in a container is discarded on restart &mdash; see [Optional Settings - Data Protection](#optional-settings---data-protection). Keys are stored unencrypted at rest, so filesystem permissions are the control; operators wanting defence in depth should additionally protect the ring with a KMS or certificate (`ProtectKeysWith*`). External-login correlation cookies are pinned to `SameSite=Strict` and `CookieSecurePolicy.Always`, and the entire pipeline runs behind `app.UseHttpsRedirection()`. ASP.NET Core's automatic anti-forgery token validation is active for the Razor Pages / Blazor UI. |
| 7 | **R**epudiation | Administrative actions (approve / delete nodesets, manage users, issue API keys) and DPP access | A user denies performing a destructive action, or denies reading controlled data, because actions are not auditable. | All privileged endpoints sit behind authenticated identities (Identity user or federated principal) so every request is bound to a `User.Identity.Name`. The upload pipeline records the uploader's identity (`_database.UploadNamespaceAndNodesetAsync(User.Identity.Name, ...)`). Every DPP read and modify, and every role grant/revoke, is additionally written to the hash-chained audit log (`IDppAuditLog` / `DppAuditLog`) bound to the acting operator; mutations write an `Attempted` record before the change and the outcome after, and a request whose audit entry cannot be committed fails with `503` rather than completing unlogged. Configure `Dpp__Audit__HmacKey` to authenticate the chain &mdash; **without it the digests are unkeyed, so anyone able to write to the audit tables can recompute them and pass verification**. The tail checkpoint (length plus last entry hash) is authenticated with the same key, so truncating the log to an earlier valid prefix and restating the checkpoint to match it no longer verifies. See [what the audit log does and does not prove](dpp.md#audit-log). Application logging is enabled via `services.AddLogging(builder => builder.AddConsole())` and emits structured logs that can be shipped to a central SIEM/Log Analytics workspace from the container host. |
| 8 | **R**epudiation | External OAuth callback (`/Account/ExternalLogin`, `OAuthEvents.OnCreatingTicket`) | A replayed or forged ticket is accepted as a legitimate sign-in. | The OAuth handler enforces correlation cookies (`CorrelationCookie.SameSite = Strict`, `SecurePolicy = Always`), uses HTTPS-only token endpoints, calls `EnsureSuccessStatusCode()` on the userinfo response, and stamps a `TicketCreated` token into the authentication properties so the time of issuance is preserved alongside the access token. |
| 9 | **I**nformation disclosure | Stored user secrets (passwords, API keys, external tokens) | DB compromise leaks credentials usable elsewhere. | Passwords are stored as PBKDF2 hashes by ASP.NET Core Identity (`AddDefaultIdentity<IdentityUser>`). API keys are issued through `ApiKeyTokenProvider` (an Identity `IUserTwoFactorTokenProvider`) and validated server-side by `UserService.ValidateApiKeyAsync`; they are not echoed back to the user after creation. OAuth refresh/access tokens stored via `SaveTokens = true` are protected by ASP.NET Core Data Protection. |
| 10 | **I**nformation disclosure | Configuration / secrets surface | Secrets such as `ServicePassword`, `EmailSenderAPIKey`, `OAuth2ClientSecret`, `Authentication:Microsoft:ClientSecret`, `CaptchaSettings__SecretKey` and the PostgreSQL password leak via source control or logs. | All secrets are read from `IConfiguration` (environment variables / mounted secret stores) and are never committed to the repository. The README explicitly documents the env-var contract (`ServicePassword`, `EmailSenderAPIKey`, `Authentication:Microsoft:ClientSecret`, `OAuth2ClientSecret`, `CaptchaSettings__SecretKey`, `ConnectionStrings__CloudLibraryPostgreSQL`). The development-only exception page is gated by `env.IsDevelopment()` so stack traces are not returned in production. |
| 11 | **I**nformation disclosure | Network traffic to/from the server | Credentials, cookies or API keys captured on the wire. | `app.UseHttpsRedirection()` forces TLS for every request. External login cookies are marked `Secure`. Containers are expected to be fronted by a TLS-terminating reverse proxy / load balancer. The embedded OPC UA `SimpleServer` (`UAClientServer/SimpleServer.cs`) uses the standard `Opc.Ua.Configuration.ApplicationInstance` certificate store so that the `opc.tcp` channel is signed and encrypted. |
| 12 | **D**enial of service | Public registration / login / password-reset endpoints | Bots flood self-registration, exhaust the email quota, or brute-force passwords. | Self-registration can be disabled entirely via `AllowSelfRegistration=false`. Google reCAPTCHA v3 is enforced through `CaptchaValidation` (configurable score via `CaptchaSettings__BotThreshold`) on registration. Identity's built-in lockout (`Lockout.cshtml`) blocks password brute-force. Email sending is delegated to Postmark or SendGrid (`PostmarkEmailSender`, `SendGridEmailSender`) which apply provider-side rate limits. |
| 13 | **D**enial of service | Large or malicious uploads, expensive nodeset parsing | A caller uploads many huge nodesets to fill storage or pin CPU. | Upload endpoints require an authenticated identity (`ApiPolicy`) so anonymous flooding is not possible. Uploaded nodesets are streamed through `MemoryStream` and then handed to `CloudLibDataProvider.UploadNamespaceAndNodesetAsync` which deduplicates by deterministic hash (`DeterministicHash.cs`) and stores them in PostgreSQL via `DbFileStorage`. Operators should additionally configure Kestrel (`KestrelServerOptions`) request-body size limits and HTTP timeouts at the reverse proxy. |
| 14 | **D**enial of service | Embedded OPC UA server (`UAClientServer/SimpleServer.cs`, `NodesetFileNodeManager.cs`) | A malicious OPC UA client opens excessive sessions/subscriptions or sends malformed messages. | The server is built on the OPC Foundation `Opc.Ua.Server` stack which enforces session limits, message size limits and security-policy validation through the configured `ApplicationInstance`. The OPC UA application certificate is created and validated automatically by `ApplicationInstance` so unsigned channels are rejected. |
| 15 | **D**enial of service | Anonymous DPP read endpoints (`DPPLifecycleApiController`, `v1/dpps/*`) | EN 18246 requires public DPP data to be readable without login, so an unauthenticated caller can scrape or flood these routes. | The DPP endpoints carry `[EnableRateLimiting(Startup.DppRateLimitPolicy)]`, a fixed-window limiter partitioned per client IP and returning `429` when exceeded; the per-minute allowance is configurable via `Dpp__RateLimit__PermitPerMinute` (default 100). Partitioning depends on seeing the real client address, so behind a proxy the `Dpp__ForwardedHeaders__*` settings must be configured &mdash; otherwise every caller shares the proxy's bucket. Each read also issues an ESDC (an RSA signature), so the limiter bounds signing cost as well as data egress. A request-count limiter only bounds work if no single request can carry unbounded work, so `POST dppsByProductIds` caps `productIds` at 100 entries (`DPPLifecycleApiController.MaxProductIdsPerRequest`, returning `400` above that) and resolves the whole batch in one set-based query rather than one query per identifier. |
| 16 | **E**levation of privilege | Administrative endpoints (approval, user/role management) | A regular user escalates to administrator and approves or deletes arbitrary nodesets, or an administrator is locked out so nobody can administer the system. | Administrative operations are protected by the `AdministrationPolicy` defined in `Startup.ConfigureServices` (`policy.RequireRole(Roles.Administrator)`, value `"Administrator"`). The role can only be assigned by an existing administrator via `AccessController`, and the bootstrap admin credentials are supplied out-of-band via the `ServicePassword` (and optional `ServiceUsername`, default `admin`) environment variables. API-key principals only carry the claims of the user that minted them, so a compromised key cannot exceed that user's role set. The role itself is also protected against self-inflicted lockout: deleting the `Administrator` role, or revoking it from the last remaining administrator, is refused with `403`, because neither is recoverable in-band once the accounts able to repair it have lost access. The last-administrator check runs under a PostgreSQL advisory lock so two concurrent revocations cannot both observe a safe count and leave none. Role claims are embedded in the authentication cookie at sign-in, so revoking a role (or deleting one) also bumps the affected users' Identity security stamp; `SecurityStampValidatorOptions.ValidationInterval` is set to one minute, which bounds how long an already-issued cookie can keep passing role checks. Revocation is therefore effective within that window rather than instantly &mdash; deployments needing immediate cut-off should shorten the interval, at the cost of a database round-trip per request. |
| 17 | **E**levation of privilege | Authentication-handler bypass | A bug in a custom authentication handler grants access without valid credentials. | The custom handlers (`BasicAuthenticationHandler`, `SignedInUserAuthenticationHandler`, `ApiKeyAuthenticationHandler`) all delegate credential verification to `UserService` which uses the Identity `UserManager`/`SignInManager` APIs (PBKDF2 password verification, normalised user lookup, time-constant comparisons). Authentication failures consistently return `AuthenticateResult.Fail/NoResult` and never short-circuit the pipeline as success. The combined `ApiPolicy` requires `RequireAuthenticatedUser()` so a `NoResult` from one scheme cannot be interpreted as success. |
| 18 | **I**nformation disclosure | Role-controlled DPP data elements (`DPPService`, `DppControlledElements`) | EN 18246 requires different economic operators to see different subsets of the same passport, so a caller reads elements their role is not entitled to. | Every DPP leaving the service is passed through `DPPService.FilterForRolesAsync`, which drops elements whose `controlledElements` entry does not grant the caller's roles; the filter is applied on the read path itself rather than in the UI, so REST, GraphQL and browse responses are filtered identically. `PATCH` responses are re-filtered after the write so a mutation cannot echo back data the caller could not have read. Writes are separately gated by `CanWriteElementAsync`, and nodeset-level visibility is enforced by `IsNodesetAccessibleAsync` before element filtering runs. Elements with no `controlledElements` entry are treated as public by design &mdash; access is deny-by-default only for elements explicitly placed under control. Raw browse surfaces bypass this filtering entirely because they return the untyped value dictionary rather than a typed DPP, so `BrowserController`'s export is restricted to the nodeset owner instead. Historical reads (`versions/{date}`) filter against the policy archived with that version rather than the current mapping, so removing an element from `controlledElements` does not retroactively publish it in older snapshots. Filtering descends every element container (`DataElementCollection.Elements` and `MultiValuedDataElement.Value`) via the shared `ChildElementsOf` helper, so a controlled element nested in a multi-valued element cannot survive into the response or the signed ESDC. An unavailable policy source is treated as deny-all rather than as an absent mapping. |
| 19 | **S**poofing | ESDC verification / trust anchors (`RsaEsdcService`) | A peer presents a self-signed ESDC claiming to originate from another economic operator, and the server accepts it as that operator's attestation. | ESDCs are W3C VC-JWT signed with RS256. Trust anchors are configured issuer-bound via `Dpp__Esdc__TrustedIssuers__N__{Issuer,PublicKeyPem,KeyId}`, and `AnchorMayVouchFor` requires the anchor to match the **signed** `issuer` claim (and the `kid`, when pinned) before the signature is accepted &mdash; so holding a trusted key does not let an operator sign for a different issuer. The issuer and `validFrom` are read from the signed payload, never from the unprotected header. Where a certificate is supplied, `EnsureCertificateMatchesSigningKey` verifies it binds to the signing key. The legacy unbound `Dpp__Esdc__TrustedPublicKeysPem` list is still honoured for compatibility but **any key in it can vouch for any issuer**; prefer the issuer-bound form. On the issuing side, `Dpp__Esdc__EconomicOperatorId` is **required** wherever a stable signing key is configured: the server refuses to start without it, refuses to sign a DPP naming a different operator, and binds its own key to that identifier so the key cannot act as a universal anchor. Making it optional would have left the impersonation open for any deployment that omitted it. See [ESDC issuer binding](dpp.md#issuer-binding). |

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
| Brute Force (1M keys) | 16 minutes | 1.7 days | **~99% slower**  |
| DOS (1000 req/sec) | Server overload | Max ~6-7 req/sec | **~99.3% reduction**  |
| Timing Analysis | Exploitable | Fixed timing | **Mitigated**  |

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

