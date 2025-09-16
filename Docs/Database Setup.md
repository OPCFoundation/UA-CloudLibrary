# UA Cloud Library - Database Setup

CloudLibrary uses PostgreSQL. Currently tested with version 14.

If you are developing locally we recommend using Docker and the provided docker-compose.yml file. This file will create a PostgreSQL instance with the following settings:

1 Creating Empty Database
-------------

An empty database is created when either the CloudLibrary app is started, or when unit tests are run. CloudLibrary has an Entity-Framework driven database. During start up, the EF migration mechanism gets summoned and it creates a PostgreSQL database, then populates the database with a set of empty tables, and initializes the required set of foreign keys.

2 Populating Database
-------------

There are two ways to populate the database:

2.1 Using the unit tests in the CloudLibClient.Tests project
-------
 
Populating the database can be accomplished by setting the database connection string, then running the tests in the CloudLibClient.Tests project. 
Each file in the following folder contains a single nodeset (a.k.a. "namespace", or "profile"):
```
.\Tests\CloudLibClientTests\TestNamespaces
```
Note that the test files are JSON formatted files that contain XML nodeset data.

2.2 Using the CLoudLibSync command-line tool
--------------

Alternatively, the database can be populated using the command-line CloudLibSync tool that is part of this repo. This approach does not use the database connection string, but instead requires a URL for the CloudLibrary and a username/password.

If you don't have a UA Cloud Library account yet, you can create one here: https://uacloudlibrary.opcfoundation.org/Account/Register

Once registered, you can use the CloudLibSync Tool to upload nodesets to your database:

The CloudLibSync tool is found in the following folder:
```
.\Tools\CloudLibSync
```

Download all Nodesets from the official OPC Foundation Cloud library:
```
CloudLibSync download https://uacloudlibrary.opcfoundation.org <yourUserName> <yourpassword> 
```

This will create a folder called "DownloadedNodesets" in the current directory, and download all nodesets from the official OPC Foundation Cloud Library into that folder.

```
CloudLibSync upload https://localhost:8443 <yourUserName> <yourpassword> 
```

When uploading nodesets to the database, this tool uses the JSON formatted files mentioned above. This tool can also be used to download nodesets in both JSON format (which includes all namespace details, including the XML) and the XML-only format.

3 Using PGAdmin
-------------
If you want to access the database admin (PGAdmin) to the development database instance open http://localhost:8088/ in your browser. You will need to register a new server in PGAdmin with the following settings:
* Name: uacloudlib
* Host name/address: db
* Port: 5432
* Maintenance database: uacloudlib
* Username: uacloudlib
* Password: uacloudlib

4 Connection Settings
-------------
If you are using the provided docker-compose.yml file, the PostgreSQL instance is created automatically. 

To set the corresponding connection string to connect to the docker-based PostgreSQL database, right click the UA-CloudLibrary Project and select "Manage User Secrets".
Then add the following connection string:

```
  "ConnectionStrings:CloudLibraryPostgreSQL": "Server=db; Username=uacloudlib;Database=uacloudlib;Port=5432;Password=uacloudlib;Include Error Detail=true"
```

5 Database Tables Explained
-------------

Once the database is created the following tables are created in the database:

* **AspNetRoles**: Contains the roles for the user management (e.g. "Admin", "User")
* **AspNetUserRoles**: Contains the mapping between users and roles
* **AspNetUsers**: Contains the user accounts
* **AspNetUserClaims**: Contains claims for users
* **AspNetUserLogins**: Contains login information for users
* **AspNetUserTokens**: Contains tokens for users

5.1 Type Related Tables
* **BaseTypes**: Contains the base types for OPC UA (e.g. BaseObjectType, BaseVariableType)
* **DataTypes**: Contains the data types for OPC UA (e.g. Int32, String)
* **ReferenceTypes**: Contains the reference types for OPC UA (e.g. HasComponent, HasProperty)
* **ReferenceTypes_InverseName**: Contains the reverse reference 
* **VariableTypes**: Contains the variable types for OPC UA (e.g. BaseDataVariableType, PropertyType)
* **ObjectTypes**: Contains the object types for OPC UA (e.g. BaseObjectType, FolderType)

5.2 Instance Related Tables
* **Methods**: Contains the methods for OPC UA (e.g. AddFolder, DeleteFolder)
* **Objects**: Contains the objects for OPC UA (e.g. Folders, Devices)
* **Variables**: Contains the variables for OPC UA (e.g. Temperature, Pressure)
* **References**: Contains the references for OPC UA (e.g. HasComponent, HasProperty)

5.3 Other Tables
* **UaEnumField**: Contains the enums for OPC UA 
* **DBFiles**: Contains the DB files 
* **InterfaceModelNodes**: Contains the interface model nodes for OPC UA (e.g. Nodes, References)

5.4 Main NodeSet Tables
* **NodeSets**: Contains the nodesets for OPC UA 
* **Nodes**: Contains all the nodes of all nodesets 
* **Nodes_Description**: tbd
* **Nodes_DisplayName**: tbd
* **Nodes_OtherReferencedNodes**: tbd
* **Nodes_OtherReferencingNodes**: tbd


