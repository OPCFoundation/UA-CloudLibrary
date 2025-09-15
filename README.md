# UA Cloud Library

The reference implementation of the UA Cloud Library. The UA Cloud Library enables the storage in and querying of OPC UA Information Models from anywhere in the world. 

## Features

* REST interface
* Swagger UI
* User management UI
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

The OPC UA CloudLib Website should open in the browser.

If you want to access the admin to the develpoment database instance open http://localhost:8080/ in your browser


## Cloud Hosting Setup

### Required Settings - PostgreSQL
You **must** have installed PostgreSQL version 11.20. You **must** also define one of the following two sets of environment variables:

#### PostgreSQL Set 1: Three environment variables
* ``PostgreSQLEndpoint``: The endpoint of the PostgreSQL instance (that must be previously deployed in the hosting platform).
* ``PostgreSQLUsername``: The username to use to log in to the PostgreSQL instance.
* ``PostgreSQLPassword``: The password to use to log in to the PostgreSQL instance.

#### PostgreSQL Set 2: One connection string
*  ``ConnectionStrings__CloudLibraryPostgreSQL``: All of the above values, as a connection string instead of as individual environment variables. Example:
```
"Server=localhost; Username=MyUserName;Password=MyUserPassword;Database=uacloudlib;Port=5432;Include Error Detail=true",
```
Note that you must create a user account with set privileges to access the database.

### Setting Password for Test Account
To enable access, from both Swagger and the REST APIs, you must set a password using this environment variable:
* ``ServicePassword``: The administration password for Swagger and REST service.

Note: The user name is ``admin``.

### Optional Settings
Environment variables that **can optionally** be defined:

* ``EmailSenderAPIKey``: The API key for the email sender service
* ``RegistrationEmailFrom``: The "from" email address to use for user registration confirmation emails
* ``RegistrationEmailReplyTo``: The "replyto" email address to use for user registration confirmation emails

* ``CloudLibrary__ApprovalRequired``: Whether items are filtered based on ``Approval Status``
* ``AllowSelfRegistration``: Whether users can self-register for a user account (default: ``true``).

### Optional Settings - Captcha
Curtail bot access using the using Google reCAPTCHA.  
  **Note: If you enable reCAPTCHA without an active account, it breaks user self-registration.**
* ``CaptchaSettings__Enabled``: Toggle whether to use reCAPTCHA (default: ``false``). 
* ``CaptchaSettings__SiteVerifyUrl``: Verify user input (default: ``https://www.google.com/recaptcha/api/siteverify``)
* ``CaptchaSettings__ClientApiUrl``: Source for loading JavaScript library (default:``https://www.google.com/recaptcha/api.js?render=``)
* ``CaptchaSettings__SecretKey``: Private key. Obtain from reCAPTCHA admin console.
* ``CaptchaSettings__SiteKey``: Public key. Obtain from reCAPTCHA admin console.
* ``CaptchaSettings__BotThreshold``: Minimum score between 0.0 (bot likely) and 1.0 (human likely). (default: ``0.5``)

    **Note: A double underscore ('__') in environment variable keys creates nested configuration sections (hierarchical keys).**

## Deployment

Docker containers are automatically built for the UA Cloud Library. The latest version is always available via:

`docker pull ghcr.io/opcfoundation/ua-cloudlibrary:latest`

## Build Status

[![Docker Image CI](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml)

[![.NET](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml)

