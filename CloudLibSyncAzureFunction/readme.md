To configure the CloudLib Sync function, credentials for the source and target cloud libraries need to be provided.

For cloud hosting, add them in in the Azure portal.

For local development, add them to the local.settings.json:

```json
{
  "Values": {
     // ...
    "CloudLibrarySync:Source:Url": "https://uacloudlibrary.opcfoundation.org/",
    "CloudLibrarySync:Source:Username": "<yourUsername>",
    "CloudLibrarySync:Source:Password": "<yourPassword>",
    "CloudLibrarySync:Target:Url": "https://localhost:5001/",
    "CloudLibrarySync:Target:Username": "<yourOtherUsername>",
    "CloudLibrarySync:Target:Password": "<yourOtherPassword>"
  }
}
```
