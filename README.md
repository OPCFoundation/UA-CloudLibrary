# UA-CloudLibrary

Reference implementation of the UA Cloud Library.


## Setup

Environment variables that must be defined:

* HostingPlatform: The cloud hosting platform. Valid options are Azure, AWS and GCP.
* BlobStorageConnectionString: The connection string to the cloud storage instance (that must be previously deployed in the hosting platform).
* PostgreSQLEndpoint: The endpoint of the PostgreSQL instance (that must be previously deployed in the hosting platform).
* PostgreSQLUsername: The username to use to log in to the PostgreSQL instance.
* PostgreSQLPassword: The password to use to log in to the PostgreSQL instance.
* ServicePassword: The administration password for the REST service (username admin).


Additional optional environment variables that can be defined when hosting on AWS:

* AWS_REGION: The AWS region used for the cloud storage instance.
* AWSRoleArn: The AWS role to use to log in to the cloud storage instance.

