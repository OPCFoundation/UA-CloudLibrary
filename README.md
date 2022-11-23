# UA Cloud Library

The reference implementation of the UA Cloud Library. The UA Cloud Library enables the storage in and querying of OPC UA Information Models from anywhere in the world.

## Features

* REST interface
* GraphQL interface
* Swagger UI
* GraphQL UI
* User management UI
* Cross-platform: Runs on Microsoft Azure, Amazon Web Services and Google Cloud Platform

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

* HostingPlatform: The cloud hosting platform. Valid options are Azure, AWS and GCP.
* BlobStorageConnectionString: The connection string to the cloud storage instance (that must be previously deployed in the hosting platform).
* PostgreSQLEndpoint: The endpoint of the PostgreSQL instance (that must be previously deployed in the hosting platform).
* PostgreSQLUsername: The username to use to log in to the PostgreSQL instance.
* PostgreSQLPassword: The password to use to log in to the PostgreSQL instance.
* ServicePassword: The administration password for the REST service (username admin).
* DataProtectionBlobName: The name of the blob storage used for the .Net data protection feature

Environment variables that **can optionally** be defined:

* EmailSenderAPIKey: The API key for the email sender service
* RegistrationEmailFrom: The "from" email address to use for user registration confirmation emails
* RegistrationEmailReplyTo: The "replyto" email address to use for user registration confirmation emails
* UseSendGridEmailSender: Use SendGrid for sending emails instead of the default Postmark

Hosting on AWS requires the identity/role used to have policies allowing access to the S3 bucket and SSM Parameter Store.

Hosting on GCP requires an identity used to have policies allowing access to the GCS bucket.
In case file based authentication is used, please set the envionment variable GOOGLE_APPLICATION_CREDENTIALS pointing to the SA-Key.

## Deployment

Docker containers are automatically built for the UA Cloud Library. The latest version is always available via:

docker pull ghcr.io/opcfoundation/ua-cloudlibrary:latest

## Build Status

[![Docker Image CI](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml)

[![.NET](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/dotnet.yml)

