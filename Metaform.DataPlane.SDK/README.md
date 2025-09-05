# Dataplane SDK .NET

![dotnet-logo](./docs/logo/net.sdk.stacked.svg)

A Data Plane SDK for .NET. This SDK provides components for creating .NET-based data planes that interface with Control
Planes via the Data Plane Signaling API (DPS API). The SDK includes callbacks on API events, transactional persistence
and mutual authentication and authorization scaffolding.

All sample code discussed here is available in the [`DataPlane.Sdk.Example.Web`](DataPlane.Sdk.Example.Web/) project.

<!-- TOC -->
* [Dataplane SDK .NET](#dataplane-sdk-net)
  * [1. Installation and requirements](#1-installation-and-requirements)
  * [2. Usage (with API)](#2-usage-with-api)
    * [2.1 Configuring the SDK](#21-configuring-the-sdk)
    * [2.2 Configuring SDK services](#22-configuring-sdk-services)
    * [2.3 Setting up authentication for incoming requests](#23-setting-up-authentication-for-incoming-requests)
    * [2.4 Setting up authorization of incoming HTTP requests](#24-setting-up-authorization-of-incoming-http-requests)
    * [2.5 Setting up authorization of outgoing HTTP requests](#25-setting-up-authorization-of-outgoing-http-requests)
      * [Named vs unnamed `HttpClient`](#named-vs-unnamed-httpclient)
  * [3. Usage (core only)](#3-usage-core-only)
  * [4. Required configuration](#4-required-configuration)
  * [5. DataPlane Signaling API callbacks](#5-dataplane-signaling-api-callbacks)
  * [6. In-memory vs PostgreSQL persistence](#6-in-memory-vs-postgresql-persistence)
  * [7. Using the Control API](#7-using-the-control-api)
  * [8. Reporting issues and bugs](#8-reporting-issues-and-bugs)
<!-- TOC -->

## 1. Installation and requirements

This SDK is compiled against `net9.0` so consuming applications must be upgraded to that as well.

To install the SDK, add the following packages to your .NET app:

* install the project's NuGet feed `https://nuget.pkg.github.com/metaform/index.json` (
  see [details](https://docs.github.com/en/packages/working-with-a-github-packages-registry/working-with-the-nuget-registry))
* `dotnet add package DataPlane.DataPlane.Sdk.Api --version 0.0.1-alpha2` for the API extensions s
* `dotnet add package DataPlane.Sdk.Core --version 0.0.1-alpha2` for the SDK core, can be omitted if `DataPlane.Sdk.Api`
  is used

Note that while the `DataPlane.Sdk.Api` package is not strictly required, it handles all incoming DPS API communication,
so it
should only be omitted if a custom API implementation is used. See [this chapter](#3-usage-core-only) for details.

> The SDK is currently hosted on GitHub's NuGet feed, which requires authorization!

## 2. Usage (with API)

This is what most SDK users will want. The `DataPlane.Sdk.Api` package adds web controllers to the app that handle
incoming DPS
requests and invokes callbacks on the `DataPlaneSdk` object.

A very bare-bones new `webapi` project would look like this (top-level statements, no `Program` class):

```csharp
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.Run();
```

### 2.1 Configuring the SDK

This example uses .NET's built-in webserver Kestrel to service any API requests and the recommended way is to register
all SDK-related services in an extension method. Let's write a simple extension method:

```csharp
using DataPlane.Sdk.Core;
using DataPlane.Sdk.Core.Data;
using DataPlane.Sdk.Core.Domain.Messages;
using DataPlane.Sdk.Core.Domain.Model;
using Void = DataPlane.Sdk.Core.Domain.Void;

namespace MyProject;

public static class MyExtensions
{
    public static void AddDataPlaneSdk(this IServiceCollection services)
    {
        // initialize and configure the DataPlaneSdk
        var config = configuration.GetSection("DataPlaneSdk").Get<DataPlaneSdkOptions>() ?? throw new ArgumentException("Configuration invalid!");
        var sdk = new DataPlaneSdk
        {
            DataFlowStore = DataFlowContextFactory.CreatePostgres(configuration, config.RuntimeId),
            RuntimeId = config.RuntimeId,
            OnStart = f => StatusResult<DataFlowResponseMessage>.Success(new DataFlowResponseMessage { DataAddress = f.Destination }),
            OnRecover = _ => StatusResult<Void>.Success(default),
            OnTerminate = _ => StatusResult<Void>.Success(default),
            OnSuspend = _ => StatusResult<Void>.Success(default),
            OnProvision = f => StatusResult<IList<ProvisionResource>>.Success([])
        };
    }

    //... more init code
}
```

There are several noteworthy things going on:

1. Binding the application config (`appsettings[.*].json`) to a configuration object (
   see [this chapter](#4-required-configuration) for details)
2. Registering API callbacks: these are invoked when respective DPS API requests are received (
   see [this chapter](#5-dataplane-signaling-api-callbacks) for details)
3. Initialization of the PostgreSQL-based data storage (see [this chapter](#6-in-memory-vs-postgresql-persistence) for
   details)

### 2.2 Configuring SDK services

The DataPlane SDK integrates well with .NET's dependency injection mechanism, and to make the most of that, its services
are registered with the DI container (the `IHost`):

```csharp
// read required configuration from appsettings.json to make it injectable
services.Configure<ControlApiOptions>(configuration.GetSection("DataPlaneSdk:ControlApi"));

// add SDK core services
services.AddSdkServices(sdk);
```

Registering the `ControlApiOptions` object is necessary, because other services will want to inject it to read
configuration.

The `AddSdkServices` extension method is provided by the SDK and registers SDK services like persistence, token
providers and API clients.

### 2.3 Setting up authentication for incoming requests

Next, we need to configure API authentication and authorization. The SDK does bring most of the scaffolding and glue
code, but clients still need to implement the following:

* API authentication logic: validating incoming auth tokens and their signatures
* authorization of outgoing HTTP requests: this is relevant when the data plane sends DPS or other HTTP requests to the
  control plane: an authorization token header must be added.

```csharp
// wire up ASP.net authentication services
services.AddSdkAuthentication(configuration);
```

this sets up default SDK token validation, which will validate:

* the issuer (valid issuer is configured via `Token:ValidIssuer`)
* the audience (`Token:ValidAudience`)
* the token signing key
* token lifetime
* token replay (`jti` claims)

In cases where a third-party IdP like KeyCloak is used, this can be customized. Instead of using the
`AddSdkAuthentication` method, authentication parameters must be overridden:

```csharp

// overwrite SDK authentication with KeycloakJWT. Effectively, this sets the default authentication scheme to "KeycloakJWT", foregoing the SDK default authentication scheme ("DataPlaneSdkJWT").
services.AddAuthentication("KeycloakJWT")
        .AddJwtBearer("KeycloakJWT", options =>
        {
            // Configure Keycloak as the Identity Provider
            options.Authority = "http://localhost:8080/realms/master";
            options.RequireHttpsMetadata = false; // Only for dev!
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = "http://localhost:8080/realms/master",
                ValidateAudience = true,
                ValidAudience = "dataplane-api", // or whatever is configured in KeyCloak
                ValidateIssuerSigningKey = true,
                ValidateLifetime = true,
                ValidateActor = false,
                ValidateTokenReplay = true
            };
        });
```

Note that this example assumes that KeyCloak is running on `localhost:8080` and has a client configured with an audience
mapper injecting `"aud" : "dataplane-api"` into the JWT. Details of how to do that can be obtained from KeyCloaks
documentation.

### 2.4 Setting up authorization of incoming HTTP requests

All resources that the DataPlane Signaling API are protected with access control. Please add the following line to your
`Program.cs` to enable authz:

```csharp
services.AddSdkAuthorization();
```

**Omitting this will cause the DataPlane Signaling API to be unprotected!**

This registers authorization handlers for all resource types, that reject any request, where the `participantContextId`
does not match the auth token's `sub` claim, for example:

* `/api/v1/participant123/dataflows/dataflowXYZ/state` and `sub: participant123` -> accepted, if `participant123` owns
  `dataflowXYZ`
* `/api/v1/participant123/dataflows/dataflowXYZ/state` and `sub: participant456` -> rejected

### 2.5 Setting up authorization of outgoing HTTP requests

The data plane needs to send HTTP requests to the control plane on several occasions, for example when sending
asynchronous DPS messages, or to register and un-register the data plane with the control plane.

These requests must be authenticated, i.e. carry an `Authorization: Bearer ey...` header. Fortunately, the DataPlane SDK
handles this centrally using the `ITokenProvider` interface.

To configure this, add the following to your extension method or `Program.cs`:

```csharp
services.AddSingleton<ITokenProvider, MyTokenProvider>();
```

it is imperative to register the provider as singleton, so that the default (no-op) token provider from the SDK gets
overwritten properly. The token provider's job is to obtain an access token from a third-party IdP such as KeyCloak. The
specifics of that are beyond the scope of this document, but the following general sequence could be implemented:

```csharp
public class MyTokenProvider(HttpClient httpClient) : ITokenProvider
{

    public Task<string> GetTokenAsync()
    {
        var clientId = GetSecretFromVault("client_id");
        var clientSecret = GetSecretFromVault("client_secret");
        var tokenEndpoint = "http://localhost:8080/realms/master/protocol/openid-connect/token";

        var request = new HttpRequestMessage(HttpMethod.Post, _tokenEndpoint);
        request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "grant_type", "client_credentials" },
            { "client_id", clientId },
            { "client_secret", clientSecret }
        });

        var response = await httpClient.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Token request failed: {response.StatusCode} - {await response.Content.ReadAsStringAsync()}");
        }

        var payload = await response.Content.ReadFromJsonAsync<TokenResponse>();
        return payload?.AccessToken ?? throw new Exception("No access token returned");

    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; }
    }
}
```

#### Named vs unnamed `HttpClient`

To avoid conflicts and potential infinite loops during token generation, the token provider is only registered for a "
named" `HttpClient` (name = `"SdkHttpClient"`). As a general rule of thumb, client code should:

* use _named_ `HttpClient` objects by using `IHttpClientFactory.CreateClient("SdkHttpClient")` when making HTTP requests
  to the DataPlane Signaling Api, the Control API or other control plane APIs
* use _unnamed_ `HttpClient` objects when making arbitrary HTTP requests to external services, like an IdP

## 3. Usage (core only)

In situations where the built-in API server for DataPlane Signaling cannot be used, it may be an option to use only the
`DataPlane.Sdk.Core` module. While this will forego all API controllers, authentication and authorization, it will still provide
core services and persistence. To do that, add the `DataPlane.Sdk.Core` package to your .NET project:
`dotnet add package DataPlane.Sdk.Core --version 0.0.1-alpha`.

Depending on the type of project (console, webapi) an `IHost` may or may not be available. If it is, client code can
still utilize the dependency injection facilities built into the SDK by calling the `AddSdkServices(sdk)` extension
method.

> The SDK should only be used in the "core-only" configuration in specific circumstances. In most cases the full SDK
> should be used.

## 4. Required configuration

The SDK makes use of .NET's configuration mechanism, specifically the `appsettings.json` that usually contains
application configuration.
We opted for combining all SDK-related configuration in one config object:

```json
{
  "DataPlaneSdk": {
    "ControlApi": {
      "BaseUrl": "http://localhost:8083/api/control"
    },
    "InstanceId": "test-dataplane-instance",
    "RuntimeId": "example-lock-id",
    "AllowedSourceTypes": [
      "test-source-type"
    ],
    "AllowedTransferTypes": [
      "test-transfer-type"
    ]
  }
}
```

With the exception of the `RuntimeId`, which is optional, all entries are _required_, and omitting them will result in a
runtime exception.

* `ControlApi.BaseUrl`: this is the base URL for the control plane's control API which is used to register and
  un-register this dataplane
* `InstanceId`: this should be a unique ID which identifies this data plane. This is used during data plane registration
* `RuntimeId`: an internal identifier that is used for various details such as database-level locking of entities
* `AllowedSourceTypes`: array of types of data sources that this data plane can handle. Influences the control plane's
  catalog.
* `AllowedTransferTypes`: array of types of transfer types that this data plane can handle. Influences the control
  plane's catalog.

If PostgreSQL persistence use used, the `appsettings.json` file must contain a connection string:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=SdkApi;Username=postgres;Password=postgres"
  }
}
```

## 5. DataPlane Signaling API callbacks

The Data Plane SDK defines several callbacks to intercept and influence DataPlane Signaling interactions. The callbacks
should be registered when [initializing the SDK](#21-configuring-the-sdk).

When using SDK callbacks, users should keep in mind the following tenets:

* all SDK callbacks are invoked _before_ objects are stored in persistence
* callbacks are always involved inside a transaction, i.e. before a call to `DbContext.SaveChanges[Async]`
* as a result, callbacks should not throw any exceptions, instead they should communicate any error using a
  `StatusResult`

## 6. In-memory vs PostgreSQL persistence

The Data Plane SDK uses the .NET EntityFramework (EF) for persistent storage, so switching between in-memory and actual
database persistence is seamless.

In most .NET applications the `DbContext` is provided via dependency injection. While the SDK does use dependency
njection, it cannot _require_ it because some applications might not use it. For this reason the `DbContext` is provided
via the [factory pattern](https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/#use-a-dbcontext-factory).

The entry point is the `DataPlaneSdk` class:

```csharp
 var sdk = new DataPlaneSdk
{
  DataFlowStore = DataFlowContextFactory.CreatePostgres(configuration, config.RuntimeId),
  // alternatively:
  // DataFlowStore = DataFlowContextFactory.CreateInMem(config.RuntimeId)

  // ...
}
```

Note that the `DbContext` is still registered as a service in the DI container if the `AddSdkServices(sdk)` extension
method is invoked.

## 7. Using the Control API

The Control API is a REST interface of the control plane, that can be used to register, un-register and delete data
plane instances.

For convenience, the SDK offers the `ControlApiService` that encapsulates API
requests, [authentication and authorization](#25-setting-up-authorization-of-outgoing-http-requests) and
deserialization.

This service is intended to be used directly from client code, as the SDK does not invoke it on its own. It does,
however, register it with the DI container.

For example:

```csharp
DataPlaneSdkOptions config = ...;
var result = await controlService.RegisterDataPlane(new DataPlaneInstance(config.InstanceId)
{
  Url = config.PublicUrl,
  State = DataPlaneState.Available,
  AllowedSourceTypes = config.AllowedSourceTypes,
  AllowedTransferTypes = config.AllowedTransferTypes
});

if(result.IsFailed)
{
  //handle error
}
```

## 8. Reporting issues and bugs
