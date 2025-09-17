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

![Architecture](https://raw.githubusercontent.com/OPCFoundation/UA-CloudLibrary/main/docs/architecture.png)

## Getting Started (Client Access)

If you want to access the globally hosted instance from the OPC Foundation at https://uacloudlibrary.opcfoundation.org from our software, you can integrate the source code from the SampleConsoleClient found in this repo. It exercises the REST API.

**Warning:** In the latest version of the REST API, a new infomodel/find2 API is introduced, returning a [UANameSpace](https://raw.githubusercontent.com/OPCFoundation/UA-CloudLibrary/refs/heads/main/Opc.Ua.CloudLib.Client/Models/UANameSpace.cs) structure, to align it with the rest of the REST API. The SampleConsoleClient is updated to work with the latest version of the REST API. Please update your client code accordingly if you were using an older version of the REST API! The older version will be removed in a future version of the API.

## Development Setup

Start development in three simple steps:

1. Checkout ``git clone https://github.com/OPCFoundation/UA-CloudLibrary.git``
2. Open with Visual Studio 2019+
3. Select ``docker-compose`` as startup project and hit F5 or the "play button"

The OPC UA CloudLib Website opens in the browser.

If you want to access the admin to the develpoment database instance open http://localhost:8080/ in your browser

## Authentication and Authorization
UA Cloud Library supports several authentication and authorization mechanisms. For access via the built-in UI, ASP.Net Core Identity is used and users can self-register using their email address, which needs to be verified. In addition, access to the UI via Azure Entra ID or Microsoft accounts can be optionally enabled via environment variables. Finally, the OPC Foundation hosted instance of the UA Cloud Library also supports access to the UI via OAuth and the OPC Foundation website user accounts. Access to the Swagger UI is also handled via ASP.Net Core Identity and users don't need to authenticate again once they are logged into the UI. The admin user account is enabled via the `ServicePassword` environment variable (see below).
Access to the REST API is handled via 1 default and 3 optional mechanisms:
* Basic authentication using the ASP.Net Core Identity user accounts. This is the default mechanism.
* Basic authentication using Azure Entra ID or Microsoft accounts, if enabled via environment variables.
* OAuth using the OPC Foundation website user accounts, if enabled via environment variables.
* API keys for service-to-service communication, if enabled via environment variables. API keys can then be created and managed via the UI.

There are only two types of user authorization policies supported by the UA Cloud Library: The Admin user and all other users. The Admin user has full access to all functionality, including user management, approving freshly uploaded OPC UA Information Models for download by everyone and deleting existing OPC UA Information Models. Users can upload, download, search, browse, and author OPC UA Information Models.
**Note: Custom roles can be added to users by the Admin user, if required by a calling service.**

Approval of freshly uploaded OPC UA Information Models for download by everyone can be optionally enabled via an environment variable (see below).

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
Note that you must create a user account with set privileges to access the database.

### Setting Password for Admin Account
To enable access, from both Swagger and the REST API, you must set a password using this environment variable:
* `ServicePassword`: The administration password for Swagger and REST service.

Note: The user name is `admin`.

### Optional Settings
Environment variables that **can optionally** be defined:

* `EmailSenderAPIKey`: The API key for the email sender service
* `RegistrationEmailFrom`: The "from" email address to use for user registration confirmation emails
* `RegistrationEmailReplyTo`: The "replyto" email address to use for user registration confirmation emails

* `CloudLibrary__ApprovalRequired`: Whether OPC UA Information Models are filtered based on approval by the Admin user.
* `AllowSelfRegistration`: Whether users can self-register for a user account (default: ``true``).

### Optional Settings - Captcha
Curtail bot access using the using Google reCAPTCHA.  
**Note: If you enable reCAPTCHA without an active account, it breaks user self-registration.**
* `CaptchaSettings__Enabled`: Toggle whether to use reCAPTCHA (default: ``false``). 
* `CaptchaSettings__SiteVerifyUrl`: Verify user input (default: ``https://www.google.com/recaptcha/api/siteverify``)
* `CaptchaSettings__ClientApiUrl`: Source for loading JavaScript library (default:``https://www.google.com/recaptcha/api.js?render=``)
* `CaptchaSettings__SecretKey`: Private key. Obtain from reCAPTCHA admin console.
* `CaptchaSettings__SiteKey`: Public key. Obtain from reCAPTCHA admin console.
* `CaptchaSettings__BotThreshold`: Minimum score between 0.0 (bot likely) and 1.0 (human likely). (default: ``0.5``)

### Optional Settings - Eclipse Dataspace Connector (EDC) Data Plane SDK
Configure the EDC Data Plane SDK to enable this instance of the UA Cloud Library as an EDC data plane.
* `DataPlaneSdk__ControlApi__BaseUrl`: the base URL to the EDC instance
* `DataPlaneSdk__InstanceId`: Unique instance ID of this EDC dataplane. Used during data plane registration with the EDC instance.
* `DataPlaneSdk__RuntimeId`: Unique runtime ID of this EDC dataplane used internally. Can be a GUID.
* `DataPlaneSdk__AllowedSourceTypes`: List of allowed sources types for this EDC dataplane. Will be used in the EDC catalog.
* `DataPlaneSdk__AllowedTransferTypes`: List of allowed transfer types for this EDC dataplane. Will be used in the EDC catalog.

**Note: A double underscore ('__') in environment variable keys creates nested configuration sections (hierarchical keys).**

## Deployment

Docker containers are automatically built for the UA Cloud Library. The latest version is always available via:

`docker pull ghcr.io/opcfoundation/ua-cloudlibrary:latest`

## Build Status

[![Docker Image CI](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml)

[![.NET](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml)

