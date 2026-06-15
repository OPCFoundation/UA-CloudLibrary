# OPC UA Cloud Library Client

A .NET client library for accessing the OPC Foundation UA Cloud Library REST API.

## Features

- Browse and search for OPC UA information models
- Download nodesets with metadata
- Upload custom nodesets (requires write permissions)
- Support for multiple authentication methods:
  - Basic Authentication (username/password)
  - API Key Authentication (Read-Only or Read-Write)

## Installation

```bash
dotnet add package Opc.Ua.Cloud.Library.Client
```

## Authentication Methods

### 1. Basic Authentication

Traditional username and password authentication:

```csharp
using Opc.Ua.Cloud.Client;

var client = new UACloudLibClient(
    "https://uacloudlibrary.opcfoundation.org",
    "username",
    "password"
);
```

### 2. API Key Authentication

API keys provide a more secure authentication method with granular permissions:

```csharp
using Opc.Ua.Cloud.Client;

// Using custom endpoint with API key
var client = new UACloudLibClient(
    "https://uacloudlibrary.opcfoundation.org",
    "CLxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxx"
);

// Using default OPC Foundation endpoint
var client = new UACloudLibClient("CLxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxx");
```

#### API Key Types

API keys come in two types:

- **Read-Only Keys**: Can only perform GET operations (browsing, searching, downloading)
- **Read-Write Keys**: Can perform all operations including POST, PUT, and DELETE (uploading, updating, deleting)

⚠️ **Important**: Attempting to perform write operations (upload, update, delete) with a Read-Only API key will result in a `403 Forbidden` response.

## Usage Examples

### Searching for Nodesets

```csharp
using Opc.Ua.Cloud.Client;
using System.Collections.Generic;

var client = new UACloudLibClient("your-api-key");

// Get the first 10 nodesets
List<UANameSpace> nodesets = await client.GetBasicNodesetInformationAsync(
    offset: 0,
    limit: 10
);

// Search with keywords
List<UANameSpace> filtered = await client.GetBasicNodesetInformationAsync(
    offset: 0,
    limit: 10,
    keywords: new List<string> { "PLCopen", "IEC" }
);

foreach (var ns in nodesets)
{
    Console.WriteLine($"Title: {ns.Title}");
    Console.WriteLine($"Namespace: {ns.Nodeset.NamespaceUri}");
    Console.WriteLine($"Identifier: {ns.Nodeset.Identifier}");
}
```

### Downloading a Nodeset

```csharp
// Download complete nodeset with XML
UANameSpace nodeset = await client.DownloadNodesetAsync("12345");

// Download metadata only (no XML)
UANameSpace metadata = await client.DownloadNodesetAsync("12345", metadataOnly: true);

// Access the nodeset XML
string xml = nodeset.Nodeset.NodesetXml;
```

### Getting Namespace Information

```csharp
// Get all namespace URIs and their identifiers
var namespaces = await client.GetNamespaceIdsAsync();
foreach (var (uri, id) in namespaces)
{
    Console.WriteLine($"{uri} -> {id}");
}

// Get extended information including version and publication date
var namespacesEx = await client.GetNamespaceIdsExAsync();
foreach (var (uri, id, version, date) in namespacesEx)
{
    Console.WriteLine($"{uri} v{version} ({date}) -> {id}");
}
```

### Uploading a Nodeset (Requires Read-Write API Key)

```csharp
// ⚠️ Requires a Read-Write API key
var client = new UACloudLibClient(
    "https://uacloudlibrary.opcfoundation.org",
    "CLxx_ReadWriteApiKey"  // Must be Read-Write, not Read-Only
);

UANameSpace myNodeset = new UANameSpace
{
    // ... populate nodeset properties
};

var (status, message) = await client.UploadNodeSetAsync(myNodeset, overwrite: false);

if (status == System.Net.HttpStatusCode.OK)
{
    Console.WriteLine("Upload successful!");
}
else
{
    Console.WriteLine($"Upload failed: {status} - {message}");
}
```

## Using with Dependency Injection

Configure the client in your `appsettings.json`:

```json
{
  "UACloudLibrary": {
    "EndPoint": "https://uacloudlibrary.opcfoundation.org",
    "ApiKey": "CLxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxx"
  }
}
```

Register in `Startup.cs` or `Program.cs`:

```csharp
services.Configure<UACloudLibClient.Options>(
    configuration.GetSection("UACloudLibrary")
);

services.AddScoped<UACloudLibClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<UACloudLibClient.Options>>().Value;

    if (!string.IsNullOrEmpty(options.ApiKey))
    {
        return new UACloudLibClient(options.EndPoint, options.ApiKey);
    }
    else
    {
        return new UACloudLibClient(options.EndPoint, options.Username, options.Password);
    }
});
```

## Using with HttpClient Factory

For better resource management and testability:

```csharp
services.AddHttpClient("UACloudLibrary", client =>
{
    client.BaseAddress = new Uri("https://uacloudlibrary.opcfoundation.org");
    client.DefaultRequestHeaders.Add("x-api-key", "your-api-key");
});

// In your service or controller
var httpClient = _httpClientFactory.CreateClient("UACloudLibrary");
var cloudLibClient = new UACloudLibClient(httpClient);
```

## API Key Security Best Practices

1. **Never commit API keys to source control** - Use environment variables or secure configuration management
2. **Use Read-Only keys when possible** - Principle of least privilege
3. **Rotate keys regularly** - Set expiration dates when creating keys
4. **Monitor key usage** - Check logs for unauthorized access attempts
5. **Delete unused keys** - Remove keys that are no longer needed

## Error Handling

```csharp
try
{
    var nodesets = await client.GetBasicNodesetInformationAsync(0, 10);
}
catch (HttpRequestException ex)
{
    // Network errors, server unavailable
    Console.WriteLine($"Request failed: {ex.Message}");
}
catch (Exception ex)
{
    // Other errors (parsing, etc.)
    Console.WriteLine($"Error: {ex.Message}");
}
```

## API Key Type Reference

| Operation | Read-Only Key | Read-Write Key |
|-----------|---------------|----------------|
| Browse/Search Nodesets | ✅ Allowed | ✅ Allowed |
| Download Nodesets | ✅ Allowed | ✅ Allowed |
| Get Namespace IDs | ✅ Allowed | ✅ Allowed |
| Upload Nodesets | ❌ Forbidden (403) | ✅ Allowed |
| Update Nodesets | ❌ Forbidden (403) | ✅ Allowed |
| Delete Nodesets | ❌ Forbidden (403) | ✅ Allowed |

## Resources

- [OPC UA Cloud Library](https://uacloudlibrary.opcfoundation.org)
- [OPC Foundation](https://opcfoundation.org)
- [API Documentation](https://uacloudlibrary.opcfoundation.org/swagger)

## License

OPC Foundation MIT License 1.00
