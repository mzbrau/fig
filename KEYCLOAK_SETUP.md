# Fig Authentication with Keycloak

Fig can be configured to use Keycloak as an OpenID Connect (OIDC) provider for authentication, delegating user management and authentication flows to Keycloak.

## Configuration

To enable Keycloak integration, you need to update settings in both the Fig API and Fig Web UI `appsettings.json` files.

### API Configuration (`src/api/Fig.Api/appsettings.json`)

Update the `ApiSettings` section:

```json
{
  "ApiSettings": {
    // ... other settings ...
    "UseKeycloak": true, // Set to true to enable Keycloak
    "KeycloakAuthority": "https://your-keycloak-server/realms/your-realm", // URL to your Keycloak realm
    "KeycloakAudience": "fig-api" // The audience value expected in JWT tokens
  }
}
```

-   `UseKeycloak`: Enables or disables Keycloak integration.
-   `KeycloakAuthority`: The URL of your Keycloak realm. Replace placeholders.
-   `KeycloakAudience`: The audience claim the API will validate in JWT tokens. This typically matches the Keycloak client ID for the API or a specific audience mapper configured in Keycloak.

### Web UI Configuration (`src/web/Fig.Web/wwwroot/appsettings.json`)

Update the `WebSettings` section:

```json
{
  "WebSettings": {
    // ... other settings ...
    "UseKeycloak": true, // Set to true to enable Keycloak
    "KeycloakAuthority": "https://your-keycloak-server/realms/your-realm", // URL to your Keycloak realm
    "KeycloakClientId": "fig-web" // The Client ID for the Fig Web UI in Keycloak
  }
}
```

-   `UseKeycloak`: Enables or disables Keycloak integration.
-   `KeycloakAuthority`: The URL of your Keycloak realm (should be the same as the API's).
-   `KeycloakClientId`: The Client ID configured in Keycloak for the Fig Web application.

## Keycloak Realm Setup

You need to configure two clients within your Keycloak realm: one for the Fig API (acting as a resource server) and one for the Fig Web UI (acting as a public client).

### 1. Client for Fig API

This client configuration allows the Fig API to validate JWT tokens issued by Keycloak.

-   **Client ID**: `fig-api` (This should match the `KeycloakAudience` in the API's `appsettings.json`).
-   **Client Protocol**: `openid-connect`
-   **Access Type**: `bearer-only` (As the API only validates tokens).
-   **Valid Redirect URIs**: Not strictly required for bearer-only, but you can set it to the base URL of your Fig API.
-   **Mappers (Optional but Recommended)**:
    -   Ensure an audience mapper is configured if your `KeycloakAudience` is different from the client ID or if you need specific audience claims.
    -   Ensure user roles are included in the access token (e.g., via "Realm Roles" or "Client Roles" mapper for the `role` or a custom claim like `client_role`/`realm_role`).

### 2. Client for Fig Web UI

This client configuration allows the Fig Web UI (Blazor WebAssembly) to authenticate users against Keycloak.

-   **Client ID**: `fig-web` (This should match `KeycloakClientId` in the Web UI's `appsettings.json`).
-   **Client Protocol**: `openid-connect`
-   **Access Type**: `public` (Blazor WASM apps are public clients).
-   **Valid Redirect URIs**:
    -   `https://your-fig-web-url/authentication/login-callback` (Replace `https://your-fig-web-url` with the actual URL of your Fig Web UI).
-   **Logout Redirect URIs**:
    -   `https://your-fig-web-url/authentication/logout-callback`
-   **Web Origins**:
    -   `https://your-fig-web-url` (or `+` to allow all valid redirect URIs).
-   **Standard Flow Enabled**: `true`
-   **Implicit Flow Enabled**: `false`
-   **Direct Access Grants Enabled**: `false`
-   **Service Accounts Enabled**: `false`

## Role Setup in Keycloak

Fig uses a simple role-based authorization model with predefined roles (e.g., `Administrator`, `User`). When using Keycloak, you need to ensure that Keycloak roles can be mapped to these Fig roles.

1.  **Create Roles in Keycloak**:
    *   It's recommended to create roles in Keycloak that directly match Fig's internal role names for simplicity (case-insensitive matching is attempted by default in the current `AuthMiddleware`). For example:
        *   `Administrator`
        *   `User`
    *   Alternatively, you can use prefixed roles like `FigAdministrator`, `FigUser`.
    *   These can be Realm Roles or Client Roles (associated with the `fig-api` client). If using Client Roles, ensure your token mappers include them.

2.  **Role Claim in Token**:
    *   The Fig API's `AuthMiddleware` will attempt to read roles from the JWT token. It looks for claims in this order:
        1.  Standard `role` claims (can be multiple).
        2.  A `client_role` claim (often used for client-specific roles).
        3.  A `realm_role` claim (often used for realm-level roles).
    *   Ensure your Keycloak client for the API is configured to include these roles in the access token via mappers. For example, you can add a "User Realm Role" mapper to the `fig-api` client scope.

3.  **Assign Roles to Users**:
    *   In Keycloak, assign the created roles to your users as needed.

If your Keycloak role names do not directly match Fig's `Role` enum values (e.g., `Administrator`, `User`), you would need to:
a.  Modify the `AuthMiddleware.cs` in the Fig API project to implement custom mapping logic between Keycloak role strings and Fig `Role` enums.
b.  Alternatively, consider adding a configurable role mapping dictionary in `ApiSettings.cs` and use that in the middleware (this has not been implemented in the current version).

By default, the first valid role claim found in the token that successfully parses to a Fig `Role` enum member will be used. If no mappable role is found, or if the role claim is missing, the user might default to the `User` role or might not be correctly authorized for role-specific features.
