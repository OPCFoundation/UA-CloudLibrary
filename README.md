# UA Cloud Library

The reference implementation of the UA Cloud Library. The UA Cloud Library enables the storage in and querying of OPC UA Information Models from anywhere in the world. 

## Features

* REST interface
* Swagger UI
* User management UI
* Cross-platform: Runs on any edge or cloud that can host a container and a PostgreSQL instance

## Getting Started (Client Access)

If you want to access the globally hosted instance from the OPC Foundation at https://uacloudlibrary.opcfoundation.org from our software, you can integrate the source code from the SampleConsoleClient found in this repo. It exercises both the GraphQL and REST API, so you have the choice.

## Development Setup

Start development in three simple steps:

1. Checkout ``git clone https://github.com/OPCFoundation/UA-CloudLibrary.git``
2. Open with Visual Studio 2019+
3. Select ``docker-compose`` as startup project and hit F5 or the "play button"

The OPC UA CloudLib Website should open in the browser.

If you want to access the admin to the develpoment database instance open http://localhost:8080/ in your browser


## Cloud Hosting Setup

Environment variables that **must** be defined:

* PostgreSQLEndpoint: The endpoint of the PostgreSQL instance (that must be previously deployed in the hosting platform).
* PostgreSQLUsername: The username to use to log in to the PostgreSQL instance.
* PostgreSQLPassword: The password to use to log in to the PostgreSQL instance.
* ServicePassword: The administration password for the REST service (username admin).

Environment variables that **can optionally** be defined:

* EmailSenderAPIKey: The API key for the email sender service
* RegistrationEmailFrom: The "from" email address to use for user registration confirmation emails
* RegistrationEmailReplyTo: The "replyto" email address to use for user registration confirmation emails


## Deployment

Docker containers are automatically built for the UA Cloud Library. The latest version is always available via:

`docker pull ghcr.io/opcfoundation/ua-cloudlibrary:latest`

## Build Status

[![Docker Image CI](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml)

[![.NET](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml)

