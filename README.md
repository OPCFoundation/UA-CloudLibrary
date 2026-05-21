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

### API Key Security Features

The UA Cloud Library implements comprehensive security measures for API key authentication to protect against various attack vectors:

#### **DOS Attack Prevention**
* **Rate Limiting:** A mandatory 500ms delay is applied to every API key validation attempt, effectively limiting attackers to **2 validation attempts per second** per connection
* **Resource Protection:** Prevents rapid-fire requests from overwhelming the server
* **CPU/Database Protection:** Reduces the load from brute-force attempts on password hashing and database queries

#### **Brute-Force Attack Mitigation**
* **Time Cost:** The 500ms validation delay makes brute-force attacks **500x slower** (from ~1000s of attempts/sec to ~2 attempts/sec)
* **Practical Impact:** To test 1 million API keys would require:
  - **Without delay:** ~16 minutes (at 1000/sec)
  - **With delay:** ~5.7 days (at 2/sec)
* **Exponential Deterrent:** Combined with account lockouts, makes attacks practically infeasible

#### **API Key Type and Expiration**
* **Access Control:** API keys can be configured as **Read-Only** or **Read-Write** to limit permissions
* **Automatic Expiration:** Keys can be set to expire after configurable periods:
  - 1 Day
  - 30 Days
  - 6 Months
  - 1 Year
  - Unlimited (no expiration)
* **Expiration Enforcement:** Expired keys are automatically rejected during validation
* **Audit Trail:** Failed attempts with expired keys are logged for security monitoring

#### **Cryptographic Security**
* **Secure Generation:** API keys are generated using `RandomNumberGenerator.GetBytes(32)` (256-bit entropy)
* **Base64URL Encoding:** Keys are encoded using Base64URL to ensure safe transmission in HTTP headers
* **Password Hashing:** Keys are hashed using ASP.NET Core Identity's PBKDF2 implementation before storage
* **Prefix Storage:** Only the first 4 characters are stored unhashed for efficient lookup while maintaining security

#### **Metadata and Auditing**
* **Metadata Format:** API key metadata is stored alongside the hash: `{prefix}{hash}|Type:{type}|Expiration:{period}|ExpiresAt:{ISO8601-date}`
* **Audit Logging:** All validation failures, expired key usage, and cache collisions are logged with warnings
* **Timing Attack Prevention:** Fixed 500ms delay is applied regardless of validation outcome (success, failure, cache hit, or cache miss)

#### **Attack Scenario Effectiveness**

| Attack Type | Without Delay | With 500ms Delay | Effectiveness |
|-------------|---------------|------------------|---------------|
| Brute Force (1M keys) | 16 minutes | 5.7 days | **99.5% slower** ✅ |
| DOS (1000 req/sec) | Server overload | Max 2 req/sec | **99.8% reduction** ✅ |
| Timing Analysis | Exploitable | Fixed timing | **Mitigated** ✅ |

#### **Performance Considerations**
* **Async Implementation:** Uses `Task.Delay()` which doesn't block threads, allowing the server to handle other requests during the delay
* **Non-Blocking:** Better than thread-blocking alternatives like `Thread.Sleep()`
* **Cache-Aware:** Even cached keys experience the validation delay, maintaining consistent security
* **Acceptable Overhead:** For REST API calls, 500ms is typically acceptable latency

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
