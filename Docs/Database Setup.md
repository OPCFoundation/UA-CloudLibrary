# UA Cloud Library - Database Setup
CloudLibrary uses PostgreSQL. Currently tested with version 14.

If you are developing locally, we recommend using Docker and the provided docker-compose.yml file. This file will create a PostgreSQL instance.

## Creating Empty Database
An empty database is created when either the CloudLibrary app is started, or when unit tests are run. CloudLibrary has an Entity Framework (EF) driven database. During start up, the EF migration mechanism gets summoned and creates a PostgreSQL database within the database server instance, then populates the database with a set of empty tables, and initializes the required set of foreign keys.

## Populating Database
There are two ways to populate the database:

### Using the unit tests in the CloudLibClient.Tests project
Populating the database can be accomplished by setting the database connection string, then running the tests in the CloudLibClient.Tests project from Visual Studio. 
Each file in the following folder contains a single nodeset (a.k.a. "namespace", or "profile"):
```
.\Tests\CloudLibClientTests\TestNamespaces
```
***Note: The test files are JSON formatted files that contain metadata about the nodeset and the actual XML nodeset data.***

***Note: If you are using Docker Desktop during development, there is a Docker setup script file located Tests/startdb.bat that will start a PostgreSQL database in a Docker container. The path within that setup script for the database initialization script init-user-db.sql must be updated to your local UA Cloud Library repository source-code path.***

### Using the CloudLibSync command-line tool

Alternatively, the database can be populated using the command-line CloudLibSync tool in Tools/CloudLibSync that is part of this repo. This approach does not use the database connection string, but instead requires a URL for the UA Cloud Library and a username/password to download nodeset files from there.

If you don't have a account to the OPC Foundation-hosted instance of the UA Cloud Library yet, you can create one here: https://uacloudlibrary.opcfoundation.org/Account/Register

Once registered, you can use the CloudLibSync Tool to download nodesets from the OPC Foundation-hosted instance of the UA Cloud Library and then upload these nodesets to your database.

Download all Nodesets from the OPC Foundation-hosted instance of the UA Cloud library:
```
CloudLibSync download https://uacloudlibrary.opcfoundation.org <yourUserName> <yourpassword> 
```

This will create a folder called "DownloadedNodesets" in the current directory, and download all nodesets into that folder.

```
CloudLibSync upload https://localhost:8443 <yourUserName> <yourpassword> 
```

When uploading nodesets to the database, this tool uses the JSON formatted files mentioned above. This tool can also be used to download nodesets in both JSON format (which includes all metadata about the nodeset, including the nodeset XML) or the nodeset XML only.

## Using PGAdmin
If you want to access the database via the PG Admin tool ([PGAdmin](https://www.pgadmin.org)) for the development database instance, open http://localhost:8088/ in your browser. You will need to register a new server in PGAdmin with the following settings:
* Name: uacloudlib
* Host name/address: db
* Port: 5432
* Maintenance database: uacloudlib
* Username: uacloudlib
* Password: uacloudlib

## Connection Settings
If you are using the provided docker-compose.yml file, the PostgreSQL instance is created automatically. 

To set the corresponding connection string to connect to the Docker-based PostgreSQL database, right click the UA Cloud Library Project and select "Manage User Secrets".
Then add the following connection string:

```
  "ConnectionStrings:CloudLibraryPostgreSQL": "Server=db; Username=uacloudlib;Database=uacloudlib;Port=5432;Password=uacloudlib;Include Error Detail=true"
```

## Database Tables Used by UA Cloud Library
Once the database is created the following tables are created in the database:

* **AspNetRoles**: Contains the roles for the user management (e.g. "Admin", "User")
* **AspNetRoleClaims**: Contains claims for user roles
* **AspNetUserRoles**: Contains the mapping between users and roles
* **AspNetUsers**: Contains the user accounts
* **AspNetUserClaims**: Contains claims for users
* **AspNetUserLogins**: Contains login information for users
* **AspNetUserTokens**: Contains tokens for users

### Type Related Tables
* **BaseTypes**: Contains the base types for OPC UA (e.g. BaseObjectType, BaseVariableType)
* **DataTypes**: Contains the data types for OPC UA (e.g. Int32, String)
* **ReferenceTypes**: Contains the reference types for OPC UA (e.g. HasComponent, HasProperty)
* **ReferenceTypes_InverseName**: Contains the reverse reference types for OPC UA (e.g. ComponentOf, PropertyOf)
* **VariableTypes**: Contains the variable types for OPC UA (e.g. BaseDataVariableType, PropertyType)
* **ObjectTypes**: Contains the object types for OPC UA (e.g. BaseObjectType, FolderType)
* **StructureField**: Contains the structure fields for OPC UA (e.g. Structure, Union)
* **UaEnumField**: Contains the enums for OPC UA

### Instance Related Tables
* **Methods**: Contains the methods for OPC UA (e.g. AddFolder, DeleteFolder)
* **Objects**: Contains the objects for OPC UA (e.g. Folders, Devices)
* **Variables**: Contains the variables for OPC UA (e.g. Temperature, Pressure)
* **Properties**: Contains the properties for OPC UA
* **References**: Contains the references for OPC UA (e.g. HasComponent, HasProperty)

### Other Tables
* **UaEnumField**: Contains the enums for OPC UA 
* **DbFiles**: Contains the OPC UA nodeset XML files and their corresponding values JSON files 
* **InterfaceModelNodes**: Contains the interface model nodes for OPC UA (e.g. Nodes, References)

### Main NodeSet Tables
* **NodeSets**: Contains the indexed nodesets for OPC UA 
* **Nodes**: Contains all the indexed nodes of all nodesets 
* **NamespaceMeta**: Contains the metadata for the nodesets (e.g. version, publication date, organization)
* **RequiredModelInfoModel**: Contains the dependent nodesets for the nodesets (e.g. required nodesets and their version)

**Note: There are a number of additional tables that are used for storing descriptions and display names of nodes which are not explicitely listed here.**
