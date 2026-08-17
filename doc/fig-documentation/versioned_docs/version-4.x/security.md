---
sidebar_position: 7
---

# Security Features

Fig has a number of security features to ensure your settings remain safe.

Features include:

- All settings values are encrypted in the database using the server secret as the encryption key
- Fig web application is protected with user credentials
- Administrators can require any user to change their password on their next login
- Fig only accepts 'good' passwords as rated by [zxcvbn](https://github.com/dropbox/zxcvbn) (Dropbox's password-strength estimator)
- Fig shows detailed [zxcvbn](https://github.com/dropbox/zxcvbn) suggestions and warnings wherever users set a new password
- Secret setting values are never sent to the Fig Web Application
- Clients must use their secret to access their settings
  - Secrets can be securely stored in a number of different locations
- All actions are logged and recorded in the database
- Offline settings files are encrypted using the client secrets
  - Offline settings can also be disabled
- File imports can be disabled
- New setting registrations can be disabled
- User roles only have access to setting and history information (minus user activity)

## SIEM Integration

Fig provides built-in integration with Security Information and Event Management (SIEM) systems through webhooks. This allows organizations to monitor security events in real-time and integrate Fig with their existing security infrastructure.

### Security Events

Fig automatically generates security events for the following activities:

- **Login attempts** (both successful and failed)
- **User creation** (when new users are registered)

Each security event includes:

- **Event Type**: The type of security event (e.g., "Login")
- **Timestamp**: UTC timestamp when the event occurred
- **Username**: The username associated with the event
- **Success**: Whether the operation was successful
- **IP Address**: The IP address of the client making the request
- **Hostname**: The hostname of the client making the request
- **Failure Reason**: If the operation failed, the reason for failure

### Setting Up SIEM Integration

To integrate Fig with your SIEM system:

1. **Create a webhook endpoint** in your SIEM system or middleware that can receive HTTP POST requests
2. **Register the webhook** in Fig by navigating to the webhooks section in the web interface
3. **Select "Security Event"** as the webhook type
4. **Configure the endpoint URL** where Fig should send security events

:::note Sentinel Integration

If you are using Microsoft Sentinel, there is a built in integration. You can find it [here](./integrations/fig-sentinel-connector.md).

:::

### Example Security Event

```json
{
  "eventType": "Login",
  "timestamp": "2024-01-15T10:30:00Z",
  "username": "admin",
  "success": false,
  "ipAddress": "192.168.1.100",
  "hostname": "workstation-01",
  "failureReason": "Invalid password"
}
```

## Rate Limiting

Fig includes configurable rate limiting to protect against denial-of-service attacks and excessive API usage. Rate limiting is implemented using ASP.NET Core's built-in rate limiting middleware with a sliding window algorithm.

Rate limiting is configured in the `ApiSettings` section of your configuration:

```json
{
  "ApiSettings": {
    "RateLimiting": {
      "GlobalPolicy": {
        "Enabled": true,
        "PermitLimit": 500,
        "Window": "00:01:00",
        "ProcessingOrder": "OldestFirst",
        "QueueLimit": 10
      }
    }
  }
}
```

- **Enabled**: Whether rate limiting is active
- **PermitLimit**: Maximum number of requests allowed within the time window
- **Window**: Time window duration (format: HH:MM:SS)
- **ProcessingOrder**: How queued requests are processed when limits are exceeded
- **QueueLimit**: Number of requests that can be queued when the limit is reached

When rate limits are exceeded, clients receive an HTTP 429 (Too Many Requests) response with a descriptive error message.

## Keycloak Authentication Mode

Fig supports running authentication in either `FigManaged` mode or `Keycloak` mode. In `Keycloak` mode, the API validates JWT bearer tokens using OIDC discovery and JWKS from the configured authority.

### API configuration

Configure `ApiSettings.Authentication`:

Development (local Keycloak over HTTP):

```json
{
  "ApiSettings": {
    "Authentication": {
      "Mode": "Keycloak",
      "Keycloak": {
        "Authority": "http://localhost:8080/realms/fig",
        "Audience": "fig-api",
        "RequireHttpsMetadata": false,
        "UsernameClaim": "preferred_username",
        "FirstNameClaim": "given_name",
        "LastNameClaim": "family_name",
        "NameClaim": "name",
        "RoleClaimPaths": [
          "groups",
          "realm_access.roles",
          "resource_access.fig.roles"
        ],
        "RoleMappings": {
          "Administrator": [ "Administrator", "/fig/Administrator" ],
          "User": [ "User", "/fig/User" ],
          "ReadOnly": [ "ReadOnly", "/fig/ReadOnly" ],
          "LookupService": [ "LookupService", "/fig/LookupService" ],
          "Dashboard": [ "Dashboard", "/fig/Dashboard" ]
        },
        "AllowedClassificationsClaim": "fig_allowed_classifications",
        "ClientFilterClaim": "fig_client_filter",
        "AdminRoleName": "Administrator"
      }
    }
  }
}
```

Production:

```json
{
  "ApiSettings": {
    "Authentication": {
      "Mode": "Keycloak",
      "Keycloak": {
        "Authority": "https://keycloak.example.com/realms/fig",
        "Audience": "fig-api",
        "RequireHttpsMetadata": true,
        "UsernameClaim": "preferred_username"
      }
    }
  }
}
```

### Web configuration

Configure `WebSettings.Authentication`:

Development (local Keycloak over HTTP):

```json
{
  "WebSettings": {
    "Authentication": {
      "Mode": "Keycloak",
      "Keycloak": {
        "Authority": "http://localhost:8080/realms/fig",
        "ClientId": "fig-web",
        "Scopes": "openid profile email",
        "ApiScope": "fig-api",
        "ResponseType": "code",
        "PostLogoutRedirectUri": "https://localhost:7148/",
        "AccountManagementUrl": "http://localhost:8080/realms/fig/account",
        "UsernameClaim": "preferred_username",
        "FirstNameClaim": "given_name",
        "LastNameClaim": "family_name",
        "NameClaim": "name",
        "RoleClaimPaths": [
          "groups",
          "realm_access.roles",
          "resource_access.fig.roles"
        ],
        "RoleMappings": {
          "Administrator": [ "Administrator", "/fig/Administrator" ],
          "User": [ "User", "/fig/User" ],
          "ReadOnly": [ "ReadOnly", "/fig/ReadOnly" ],
          "LookupService": [ "LookupService", "/fig/LookupService" ],
          "Dashboard": [ "Dashboard", "/fig/Dashboard" ]
        },
        "AllowedClassificationsClaim": "fig_allowed_classifications",
        "AdminRoleName": "Administrator"
      }
    }
  }
}
```

Production:

```json
{
  "WebSettings": {
    "Authentication": {
      "Mode": "Keycloak",
      "Keycloak": {
        "Authority": "https://keycloak.example.com/realms/fig",
        "ClientId": "fig-web",
        "Scopes": "openid profile email",
        "ApiScope": "fig-api",
        "ResponseType": "code",
        "PostLogoutRedirectUri": "https://fig.example.com/",
        "AccountManagementUrl": "https://keycloak.example.com/realms/fig/account",
        "UsernameClaim": "preferred_username"
      }
    }
  }
}
```

### Claim mapping requirements

- `FigManaged` is the default authentication mode. Keycloak is opt-in and should be enabled for both API and web together.
- `Audience` is required in API Keycloak mode and must match tokens intended for the Fig API.
- Keycloak groups are the expected access contract. By default, Fig reads `groups`, `realm_access.roles`, and `resource_access.fig.roles`, then maps those values to Fig roles through `RoleMappings`.
- Role/group claims must map to Fig roles (`Administrator`, `User`, `ReadOnly`, `LookupService`, `Dashboard`).
- `fig_allowed_classifications` should be provided as either a JSON array string or a comma-separated list.
- `fig_client_filter` must be a valid regular expression.

Fallback behavior:

- If `fig_allowed_classifications` is missing for `Administrator`, Fig grants all classifications.
- If `fig_allowed_classifications` is missing for non-admin users, access is denied.

### Brokered identity providers (e.g. Entra ID)

Fig always authenticates against Keycloak. External providers such as Microsoft Entra ID are configured in **Keycloak Admin** as identity providers (OIDC/SAML broker). Fig never talks to Entra directly.

To send users straight to a brokered IdP (skipping the Keycloak username/password page), set optional web settings:

```json
{
  "WebSettings": {
    "Authentication": {
      "Mode": "Keycloak",
      "Keycloak": {
        "Authority": "https://keycloak.example.com/realms/fig",
        "ClientId": "fig-web",
        "IdentityProviderHint": "entra-id",
        "EnableIdentityProviderHint": true,
        "LoginPrompt": "",
        "PostLogoutLoginPrompt": "select_account"
      }
    }
  }
}
```

| Setting | Purpose |
|---|---|
| `IdentityProviderHint` | Keycloak IdP alias sent as OIDC `kc_idp_hint` (must match the alias in Keycloak) |
| `EnableIdentityProviderHint` | Set to `false` to stop sending `kc_idp_hint` without removing the hint value |
| `LoginPrompt` | Optional OIDC `prompt` on normal logins; leave empty for seamless IdP SSO |
| `PostLogoutLoginPrompt` | OIDC `prompt` on the first login after logout (default `select_account`) so another account can be chosen |

**Near-seamless Windows / Entra SSO** (open Fig and land logged in with little or no prompt) depends on browser and directory setup outside Fig, for example:

- Microsoft Edge with Enterprise SSO / device Primary Refresh Token
- Entra Connect Seamless SSO (Kerberos) on domain-joined machines
- An existing Entra session cookie in that browser

Fig cannot guarantee zero-prompt login. True silent Windows PRT/WAM SSO requires the client to talk directly to Entra; that path is not supported for Blazor WASM through Keycloak.

**Logout and alternate accounts:** after logout, the next login uses `PostLogoutLoginPrompt` (default `select_account`). With `IdentityProviderHint` enabled, account selection happens at the brokered IdP (e.g. another Entra account). To allow Keycloak local users instead, set `EnableIdentityProviderHint` to `false`.

**Turning IdP redirect off:** set `EnableIdentityProviderHint` to `false`, clear `IdentityProviderHint`, or switch both API and web back to `FigManaged`.

### Endpoint behavior in Keycloak mode

- `POST /users/authenticate` returns `404`.
- Fig user-management endpoints are unavailable (`/users`, `/users/register`, `/users/{id}`).
- Machine-client endpoints continue to work with `clientSecret`.
- Reports that list Fig-managed users degrade:
  - **Access & Privilege** is built from login events in the selected range; role/filter/classification fields show `N/A (Keycloak)`.
  - **Fig Platform Self-Report** shows users as `External (Keycloak)` instead of a Fig database count.
  - **User Activity** still works; enter the Keycloak username manually (Fig user dropdowns are unavailable).
- Fig Assistant and other administrator APIs use the live Keycloak access token (refreshed via OIDC), same as other Fig.Web API calls.

### Mode switching and rollback

- To switch to Keycloak mode, set both API and web mode values to `Keycloak`.
- To roll back, set both API and web mode values to `FigManaged`.
- Keep API and web modes aligned to avoid login and token propagation mismatches.

### Aspire AppHost (local development)

- By default, `Fig.AppHost` runs with FigManaged authentication and does **not** start a Keycloak container.
- Set `"UseKeycloak": true` in `Fig.AppHost` `appsettings.json` (or `UseKeycloak=true` via user secrets / environment) to:
  - start the local Keycloak container with the sample realm import,
  - configure the API for Keycloak via environment overrides,
  - set the web environment to `Keycloak` so Blazor WASM loads `wwwroot/appsettings.Keycloak.json`.
- Leave `UseKeycloak` as `false` (the default) for the normal FigManaged login flow.

### Troubleshooting mode mismatch

- Web is `Keycloak`, API is `FigManaged`: OIDC login succeeds, but API calls fail authorization.
- Web is `FigManaged`, API is `Keycloak`: local login endpoints are unavailable (`/users/authenticate` returns `404`).
- Invalid OIDC authority/JWKS/audience causes API token validation failures.

## Forward Headers

When Fig is deployed behind a reverse proxy or load balancer, the original client IP address and protocol information may be lost. Forward headers configuration allows Fig to trust and process `X-Forwarded-For` and `X-Forwarded-Proto` headers from known proxies.

Forward headers are configured in the `ApiSettings` section:

```json
{
  "ApiSettings": {
    "TrustForwardedHeaders": true,
    "KnownProxies": [
      "192.168.1.10",
      "10.0.0.5"
    ],
    "KnownNetworks": [
      "192.168.1.0/24",
      "10.0.0.0/8"
    ]
  }
}
```

- **TrustForwardedHeaders**: Whether to process forwarded headers
- **KnownProxies**: Specific IP addresses of trusted proxies
- **KnownNetworks**: Network ranges of trusted proxies (CIDR notation)

Only forwarded headers from explicitly configured proxies and networks are trusted. This prevents IP spoofing attacks where malicious clients could manipulate forwarded headers.

## Security Recommendations

The following recommendations will ensure your application settings are as safe as possible.

1. **Use secret settings** - Secret settings are not sent down to the web client and not shown once they are entered. They should be used for passwords, keys and any other sensitive values.
1. **HTTPS everywhere** - Fig should be deployed with **HTTPS** for both API and Web. Setting values are transmitted to setting clients; HTTPS keeps those values from being intercepted in transit.
1. **Disabling the administrator login** - Fig ships with an administrator login 'admin' with 'admin' as the password. The API can be configured to require a password change for that user on first login. Administrators can also require any other user to change their password on their next login from the Users page. The default administrative user can be removed and replaced with other administrative logins.
1. **Strong Passwords** - Fig has a password rating view where you set your password. It will not accept any passwords rated worse than 'Good', and it surfaces detailed [zxcvbn](https://github.com/dropbox/zxcvbn) feedback to help users improve weak passwords.
1. **Dedicated user accounts** - Each user of fig should be allocated their own account. This will ensure the audit log accurately reflects who made the change. If all changes are made by Admin it won't add much value.
1. **SQL Server Security** - Fig uses SQLite out of the box but should be changed to SQL Server for production deployments. All setting values are encrypted in the database but it is still important that the database itself is secured.
1. **Disabling new registrations** - The Fig registration endpoint is unsecured. This means any client is able to register with Fig. It is possible to turn off new client registrations and this should be done in production once all known clients have registered with Fig.
1. **Rolling API Secret** - The API secret is used to sign login tokens as well as encrypt all settings in the database. It can be changed at any time, however the old client secret must be retained to decrypt existing values in the database. See [the guide](http://www.figsettings.com/docs/guides/api-secret-migration) for steps.
1. **Protect API Secret** - If the API secret is compromised then it will be possible to decrypt values in the database (assuming that they can access the database). It is important that it be protected either by storing it in DPAPI (Windows only) or as a docker secret.
1. **Changing client secrets** - Client secrets can be changed during runtime using the web client. Clients need to be updated within the grace period. See [the guide](http://www.figsettings.com/docs/guides/client-secret-migration) for steps.
1. **Protect client secrets** - Client secrets protect the values for that client and as a result, they should be kept secret. Fig supports five built-in secret providers (Docker, DPAPI, Azure, AWS, Google) plus `ClientSecretOverride` for development. Prefer a GUID. On Windows, DPAPI is the recommended store. See [Client Secrets](./features/28-client-secrets/1-client-secret-providers.md).
1. **Web Hook Alerts** - Setting up web hook alerts will ensure you are kept informed if settings are changed.
1. **Disable Client Overrides** - If client overrides are not being used, disable this feature or at least limit it to the clients that should have access. This can avoid unwanted consequences.
1. **Enable TLS** - Fig supports TLS for both the Web and Api instances. See [the guide](http://www.figsettings.com/docs/guides/configuring-tls) for steps and example config.
