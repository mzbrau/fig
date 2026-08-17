---
sidebar_position: 9
---

# API Configuration

The Fig API can be configured from:

- `appsettings.json`
- environment variables (`ApiSettings__Secret`, nested keys with `__`)
- Docker secrets

## `ApiSettings`

| Setting | Description | Default |
| ------- | ----------- | ------- |
| `DbConnectionString` | Database connection string. SQLite by default; any NHibernate-supported SQL database works. See [Database](./database.md). | `Data Source=fig.db;Version=3;New=True` |
| `Secret` | Used to sign auth tokens and encrypt setting values at rest. Use a long random value. | (required) |
| `TokenLifeMinutes` | Lifetime of Fig-managed JWT auth tokens, in minutes. | `10080` (7 days) |
| `PreviousSecret` | Previous API secret, used during [API secret migration](./guides/1-api-secret-migration.md). | empty |
| `SecretsDpapiEncrypted` | When `true`, `Secret` and `PreviousSecret` are DPAPI-encrypted (Windows only). | `false` |
| `WebClientAddresses` | Allowed Fig Web origins for CORS. | localhost ports used in development |
| `ForceAdminDefaultPasswordChange` | Require the default `admin` user to change password on first login. | `false` |
| `ImportFolderPath` | Absolute path for file-based imports. Empty or invalid disables file import. Supports environment-variable expansion (for example `%APPDATA%/Fig/ConfigImport`). | empty |
| `EnableGitHubReleaseDiscovery` | When `true`, the API periodically checks GitHub for newer Fig releases. Set to `false` (or `ApiSettings__EnableGitHubReleaseDiscovery=false`) on hosts without outbound internet. | `true` |
| `SchedulingCheckIntervalMs` | How often deferred / scheduled setting changes are evaluated. | `30000` |
| `TimeMachineCheckIntervalMs` | How often Time Machine checkpoints are considered. | `3600000` |
| `DisableTransactionMiddleware` | Disable per-request database transactions. Leave `false` unless you have a specific reason. | `false` |
| `OutboundHttpProxyAddress` | Explicit proxy for outbound HTTP from the API. When unset, Fig falls back to `HTTPS_PROXY` / `HTTP_PROXY` / `ALL_PROXY`. | empty |
| `HashCacheExpiryMinutes` | Cache expiry for hash validation results. `0` disables caching. | `60` |
| `TrustForwardedHeaders` | Enable ASP.NET Core forwarded-headers middleware so `Connection.RemoteIpAddress` reflects the client behind a proxy. | `false` |
| `KnownProxies` | Proxy IP addresses trusted to supply forwarded headers. | empty |
| `KnownNetworks` | CIDR ranges trusted to supply forwarded headers (for example `10.0.0.0/8`). | empty |
| `Authentication` | Fig-managed or Keycloak authentication. See [Security](./security.md#keycloak-authentication-mode). | `FigManaged` |
| `RateLimiting` | Sliding-window API rate limits. See [Security](./security.md#rate-limiting). | 500 requests / minute |

Example:

```json
"ApiSettings": {
    "DbConnectionString": "Data Source=fig.db;Version=3;New=True",
    "Secret": "76d3bd66ddb74623ad38e39d7eae6ee5da28bbdce9aa40209d0decf630777304",
    "TokenLifeMinutes": 10080,
    "PreviousSecret": "",
    "SecretsDpapiEncrypted": false,
    "WebClientAddresses": [
        "https://localhost:7148",
        "http://localhost:7148",
        "http://localhost:8080",
        "http://localhost:5050"
      ],
    "ForceAdminDefaultPasswordChange": false,
    "ImportFolderPath": "",
    "EnableGitHubReleaseDiscovery": true,
    "SchedulingCheckIntervalMs": 30000,
    "TimeMachineCheckIntervalMs": 3600000,
    "OutboundHttpProxyAddress": "",
    "HashCacheExpiryMinutes": 60,
    "TrustForwardedHeaders": false,
    "Authentication": {
      "Mode": "FigManaged"
    }
  }
```

File-based imports run only when `ImportFolderPath` is a valid, writable absolute path. JSON files placed there are processed and deleted.

GitHub release discovery is enabled by default. When disabled, the API does not call GitHub; the "new release available" highlight will not appear. Static release highlights shipped with Fig.Web are unaffected.

:::warning Security Considerations
The configured import folder path requires write access and any JSON files placed in this directory will be automatically processed and deleted by the Fig API. When configuring this path:
- Ensure the path has appropriate filesystem permissions to prevent unauthorized access
- In containerized or shared hosting environments, carefully consider path boundaries and isolation
- Avoid pointing to system directories or paths outside of your application's designated data area
- The path supports environment variable expansion (e.g., `%APPDATA%/Fig/ConfigImport`)
:::

Fig Web settings (`WebSettings`) are documented on [Web Configuration](./web-configuration.md).
