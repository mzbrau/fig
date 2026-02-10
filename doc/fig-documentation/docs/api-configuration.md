---
sidebar_position: 10

---

# API Configuration

The Fig API can be configured with the following sources:

- appsettings.json file
- environment variables
- docker secrets

There are the following settings:

```json
"ApiSettings": {
    // The connection string for the database
    "DbConnectionString": "Data Source=fig.db;Version=3;New=True",
  
    // A secret value used to sign auth tokens and encrypt data in the database. Should be long.
    "Secret": "76d3bd66ddb74623ad38e39d7eae6ee5da28bbdce9aa40209d0decf630777304",
  
    // Lifetime of auth tokens in minutes
    "TokenLifeMinutes": 10080,
  
    // Previous secret value is used when migrating from an old to new secret. See API Secret Migration.
    "PreviousSecret": "",
  
    // True if the secret and previous secret values are encrypted via dpapi. If false, the raw values are used.
    "SecretsDpapiEncrypted": false,
    
    // Addresses of the web client. This is used for CORS validation.
    "WebClientAddresses": [
        "https://localhost:7148",
        "http://localhost:8080",
        "http://localhost:5050"
      ],
    
    // True if the API should enforce a password change on the default admin user on first login.
    "ForceAdminDefaultPasswordChange": false,

    // Optional. Absolute path for file-based imports. Empty or invalid disables file import.
    "ImportFolderPath": "",
  },
```

File-based imports are only enabled when `ImportFolderPath` is set to a valid, writable absolute path. When the value is empty or invalid, the import background service is not registered and file imports are disabled.

:::warning Security Considerations
The configured import folder path requires write access and any JSON files placed in this directory will be automatically processed and deleted by the Fig API. When configuring this path:
- Ensure the path has appropriate filesystem permissions to prevent unauthorized access
- In containerized or shared hosting environments, carefully consider path boundaries and isolation
- Avoid pointing to system directories or paths outside of your application's designated data area
- The path supports environment variable expansion (e.g., `%APPDATA%/Fig/ConfigImport`)
:::

