## CloudLibSync - Command Line Helper Utility
CloudLibSync is a command-line utility that enables accessing Cloud Library data.

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
  sync <sourceUrl> <sourceUserName> <sourcePassword> <targetUrl> <targetUserName> <targetPassword>  Downloads all nodests and their metadata from a Cloud Library (source) and uploads it to another Cloud Library (target).
  download <sourceUrl> <sourceUserName> <sourcePassword>                                            Downloads all nodesets and their metadata from a Cloud Library to a local directory.
  upload <targetUrl> <targetUserName> <targetPassword>                                              Uploads nodesets and their metadata from a local directory to a cloud library.


C:\Github.OpcFoundation\UA-CloudLibrary_pyao_aas_rest_apis\CloudLibSync\bin\Debug\net9.0>
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
  CloudLibSync download <sourceUrl> <sourceUserName> <sourcePassword> [options]

Arguments:
  <sourceUrl>
  <sourceUserName>
  <sourcePassword>

Options:
  --localDir <localDir>            [default: Downloads]
  --nodeSetXmlDir <nodeSetXmlDir>  If specified the node sets without their metadata (XML only) will be written to this directory.
  -?, -h, --help                   Show help and usage information
```

### CloudLibSync Upload Options
With the *upload** option, you can upload either all files in a folder, or just a single file. Files are expected to be in JSON format, representing the complete set of items required for an entry in the CloudLibrary database.
```
CloudLibSync upload --help
Description:
  Uploads nodesets and their metadata from a local directory to a cloud library.

Usage:
  CloudLibSync upload <targetUrl> <targetUserName> <targetPassword> [options]

Arguments:
  <targetUrl>
  <targetUserName>
  <targetPassword>

Options:
  --localDir <localDir>  The local directory to store downloaded nodesets. [default: Downloads]
  --fileName <fileName>  If specified, uploads only this nodeset file. Otherwise all files in --localDir are uploaded.
  -?, -h, --help         Show help and usage information
```

### CloudLibSync Sync Options
Download the entire contents of one Cloud Library and upload it to another Cloud Library.

```
CloudLibSync sync --help
Description:
  Downloads all nodests and their metadata from a Cloud Library (source) and uploads it to another Cloud Library (target).

Usage:
  CloudLibSync sync <sourceUrl> <sourceUserName> <sourcePassword> <targetUrl> <targetUserName> <targetPassword> [options]

Arguments:
  <sourceUrl>
  <sourceUserName>
  <sourcePassword>
  <targetUrl>
  <targetUserName>
  <targetPassword>

Options:
  -?, -h, --help  Show help and usage information
```

### Sample Usage: Downloading All Contents of a CloudLibrary
Specify the **download** option to download the entire contents of a CloudLibrary, as shown here:

```
CloudLibSync download https://localhost:5001 user123 secretpw
```

Alternatively, you can download the entire contents of a CloudLibrary, with only the XML nodesets files, using a command like this:
```
CloudLibSync download https://localhost:5001 user123 secretpw --nodeSetXmlDir JustXml
```


### Sample Usage: Uploading to a CloudLibrary
To upload a single file, use a command like this. Note that if the nodeset already exists, this call will fail.
```
CloudLibSync upload https://localhost:5001 user123 secretpw --fileName "C:\_TestData\testnodeset001.json"
```
To always upload a file, overwriting if the nodeset already exists, use the **overwrite** flag like this:
```
CloudLibSync upload https://localhost:5001 user123 secretpw --fileName "C:\_TestData\testnodeset001.json" --overwrite true
```

### Sample Usage: Synchronizing Two Instances of Cloud Library
The **sync** command line option allows you to move all new items from one CloudLibrary to another CloudLibrary:
```
CloudLibSync.exe sync https://localhost:5001 Admin testpw https://localhost:5001 Admin testpw
```
