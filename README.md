# UA-CloudLibrary

The reference implementation of the UA Cloud Library. The UA Cloud Library enables the storage and querying of OPC UA Information Models from the Cloud.

## Features

* REST interface
* GraphQL interface
* Swagger UI
* GraphQL UI
* User management UI
* Cross-platform: Runs on Microsoft Azure, Amazon Web Services, Google Cloud Platform


## Setup

Environment variables that must be defined:

* HostingPlatform: The cloud hosting platform. Valid options are Azure, AWS and GCP.
* BlobStorageConnectionString: The connection string to the cloud storage instance (that must be previously deployed in the hosting platform).
* PostgreSQLEndpoint: The endpoint of the PostgreSQL instance (that must be previously deployed in the hosting platform).
* PostgreSQLUsername: The username to use to log in to the PostgreSQL instance.
* PostgreSQLPassword: The password to use to log in to the PostgreSQL instance.
* ServicePassword: The administration password for the REST service (username admin).
* SendGridAPIKey: The API key for the Sendgrid service


Additional optional environment variables that can be defined when hosting on AWS:

* AWS_REGION: The AWS region used for the cloud storage instance.
* AWSRoleArn: The AWS role to use to log in to the cloud storage instance.

[![Docker Image CI](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml/badge.svg)](https://github.com/OPCFoundation/UA-CloudLibrary/actions/workflows/docker.yml)
