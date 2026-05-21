## CloudLibSync - Command Line Helper Utility
CloudLibSync is a command-line utility that enables accessing Cloud Library data.

## Authentication

CloudLibSync supports two authentication methods:

### 1. Basic Authentication (Username/Password)
Provide username and use the `--sourcePassword` or `--targetPassword` option:
```
CloudLibSync download https://uacloudlibrary.opcfoundation.org myuser --sourcePassword mypassword
```

### 2. API Key Authentication
Provide only the API key (omit password option):
```
CloudLibSync download https://uacloudlibrary.opcfoundation.org CLxx_xxxxxxxxxxxxxxxxxxxxxxxxxxxx
```

**Important:** 
- For **download** and **sync (source)** operations, you can use either **Read-Only** or **Read-Write** API keys
- For **upload** and **sync (target)** operations, you **must** use a **Read-Write** API key
- Read-Only API keys will receive `403 Forbidden` errors when attempting write operations

### Command Line Options
The CloudLibSync utility enables you to upload data to a CloudLibrary instance, download data from a CloudLibrary instance, and synchronize the contents of two CloudLibrary instances.

View a summary of available commands using the **--help** command line option.
```
C:\> CloudLibSync --help
Description:

Usage:
  CloudLibSync [command] [options]

Options:
  --version       Show version information
  -?, -h, --help  Show help and usage information

Commands:
  sync <sourceUrl> <sourceAuth> <targetUrl> <targetAuth>  Downloads all nodesets and their metadata from a Cloud Library (source) and uploads it to another Cloud Library (target).
  download <sourceUrl> <sourceAuth>                       Downloads all nodesets and their metadata from a Cloud Library to a local directory.
  upload <targetUrl> <targetAuth>                         Uploads nodesets and their metadata from a local directory to a cloud library.
```

### CloudLibSync Download Options
With the **download** option, you can download the contents of a CloudLibrary instance in one of two file formats:
- As Namespace files (complete contents for each namespace)
- As XML files (the rudimentary XML of an OPC/UA NodeSet)

```
CloudLibSync download --help
Description:
  Downloads all nodesets and their metadata from a Cloud Library to a local directory.

Usage:
  CloudLibSync download <sourceUrl> <sourceAuth> [options]

Arguments:
  <sourceUrl>   - URL of the Cloud Library
  <sourceAuth>  - Username (Basic Auth) or API key (API key auth)

Options:
  --sourcePassword <sourcePassword>  Password (only required for Basic Auth, omit for API key)
  --localDir <localDir>              [default: Downloads]
  --nodeSetXmlDir <nodeSetXmlDir>    If specified the node sets without their metadata (XML only) will be written to this directory.
  -?, -h, --help                     Show help and usage information
```

### CloudLibSync Upload Options
With the **upload** option, you can upload either all files in a folder, or just a single file. Files are expected to be in JSON format, representing the complete set of items required for an entry in the CloudLibrary database.

**⚠️ Important:** Upload requires a **Read-Write** API key. Read-Only keys will fail with `403 Forbidden`.

```
CloudLibSync upload --help
Description:
  Uploads nodesets and their metadata from a local directory to a cloud library.

Usage:
  CloudLibSync upload <targetUrl> <targetAuth> [options]

Arguments:
  <targetUrl>   - URL of the Cloud Library
  <targetAuth>  - Username (Basic Auth) or API key (must be Read-Write for API key auth)

Options:
  --targetPassword <targetPassword>  Password (only required for Basic Auth, omit for API key)
  --localDir <localDir>              The local directory to store downloaded nodesets. [default: Downloads]
  --fileName <fileName>              If specified, uploads only this nodeset file. Otherwise all files in --localDir are uploaded.
  --overwrite                        If specified, allows existing nodesets in a Cloud Library to be overwritten.
  -?, -h, --help                     Show help and usage information
```

### CloudLibSync Sync Options
Download the entire contents of one Cloud Library and upload it to another Cloud Library.

**⚠️ Important:** Target Cloud Library requires a **Read-Write** API key. Source can use Read-Only or Read-Write.

```
CloudLibSync sync --help
Description:
  Downloads all nodesets and their metadata from a Cloud Library (source) and uploads it to another Cloud Library (target).

Usage:
  CloudLibSync sync <sourceUrl> <sourceAuth> <targetUrl> <targetAuth> [options]

Arguments:
  <sourceUrl>   - URL of the source Cloud Library
  <sourceAuth>  - Source username (Basic Auth) or API key (API key auth)
  <targetUrl>   - URL of the target Cloud Library
  <targetAuth>  - Target username (Basic Auth) or API key (must be Read-Write for API key auth)

Options:
  --sourcePassword <sourcePassword>  Source password (only required for Basic Auth, omit for API key)
  --targetPassword <targetPassword>  Target password (only required for Basic Auth, omit for API key)
  --overwrite                        If specified, allows existing nodesets in a Cloud Library to be overwritten.
  -?, -h, --help                     Show help and usage information
```

### Sample Usage: Downloading All Contents of a CloudLibrary
Specify the **download** option to download the entire contents of a CloudLibrary, as shown here:

**Using Basic Authentication:**
```
CloudLibSync download https://localhost:5001 user123 --sourcePassword secretpw
```

**Using API Key Authentication:**
```
CloudLibSync download https://uacloudlibrary.opcfoundation.org CLxx_ReadOnlyKey
```

Alternatively, you can download the entire contents of a CloudLibrary, with only the XML nodesets files, using a command like this:

**Using Basic Authentication:**
```
CloudLibSync download https://localhost:5001 user123 --sourcePassword secretpw --nodeSetXmlDir JustXml
```

**Using API Key Authentication:**
```
CloudLibSync download https://uacloudlibrary.opcfoundation.org CLxx_Key --nodeSetXmlDir JustXml
```


### Sample Usage: Uploading to a CloudLibrary
To upload a single file, use a command like this. Note that if the nodeset already exists, this call will fail unless --overwrite is specified.

**Using Basic Authentication:**
```
CloudLibSync upload https://localhost:5001 user123 --targetPassword secretpw --fileName "C:\_TestData\testnodeset001.json"
```

**Using API Key Authentication (requires Read-Write key):**
```
CloudLibSync upload https://uacloudlibrary.opcfoundation.org CLxx_ReadWriteKey --fileName "C:\_TestData\testnodeset001.json"
```

To always upload a file, overwriting if the nodeset already exists, use the **--overwrite** flag like this:

**Using Basic Authentication:**
```
CloudLibSync upload https://localhost:5001 user123 --targetPassword secretpw --fileName "C:\_TestData\testnodeset001.json" --overwrite
```

**Using API Key Authentication:**
```
CloudLibSync upload https://uacloudlibrary.opcfoundation.org CLxx_ReadWriteKey --fileName "C:\_TestData\testnodeset001.json" --overwrite
```

### Sample Usage: Synchronizing Two Instances of Cloud Library
The **sync** command line option allows you to move all new items from one CloudLibrary to another CloudLibrary:

**Using Basic Authentication for both:**
```
CloudLibSync sync https://localhost:5001 Admin https://localhost:5002 Admin --sourcePassword testpw --targetPassword testpw
```

**Using API Keys (target must be Read-Write):**
```
CloudLibSync sync https://source.example.com CLxx_SourceKey https://target.example.com CLxx_TargetReadWriteKey
```

**Mixed authentication (API key for source, Basic Auth for target):**
```
CloudLibSync sync https://uacloudlibrary.opcfoundation.org CLxx_ReadOnlyKey https://localhost:5001 Admin --targetPassword testpw
```

**With overwrite enabled:**
```
CloudLibSync sync https://source.example.com CLxx_SourceKey https://target.example.com CLxx_TargetKey --overwrite
```

## API Key Permissions Summary

| Operation | Read-Only Key | Read-Write Key | Basic Auth |
|-----------|---------------|----------------|------------|
| Download (source) | ✅ Allowed | ✅ Allowed | ✅ Allowed |
| Upload (target) | ❌ Forbidden (403) | ✅ Allowed | ✅ Allowed (with permissions) |
| Sync (source) | ✅ Allowed | ✅ Allowed | ✅ Allowed |
| Sync (target) | ❌ Forbidden (403) | ✅ Allowed | ✅ Allowed (with permissions) |
