# Auth Feature Design: Keycloak Integration with Dual Auth Modes

## 1) Goals and Non-Goals

### Goals

- Introduce Keycloak as an optional identity provider for web-user authentication.
- Preserve existing Fig-managed authentication/authorization behavior when configured.
- Enforce that, in Keycloak mode, API user endpoints trust only Keycloak-issued access tokens.
- Keep existing role-based authorization model (`Administrator`, `User`, `LookupService`, `ReadOnly`).
- Move user-centric authorization metadata management (`AllowedClassifications`, `ClientFilter`) to Keycloak claims in Keycloak mode.
- Keep application/client-secret auth flows unchanged (machine clients do **not** use Keycloak).
- Provide seamless web login redirect flow: unauthenticated users are sent to Keycloak and returned to Fig.
- Remove/hide Fig user-management UI (`/users`) in Keycloak mode.
- Add Keycloak to Aspire AppHost for local/dev workflows.

### Non-Goals

- No change to Fig client SDK authentication model for app-to-API interactions.
- No schema migration required to remove Fig users; Fig user table remains for Fig mode.
- No mixed token trust for user endpoints in Keycloak mode (Fig JWTs should not be accepted there).

---

## 2) Current State Summary (from code)

### API
- Custom middleware auth pipeline is used:
  - `AuthMiddleware` reads `Authorization` bearer token, validates via `ITokenHandler`, resolves `UserDataContract` from DB, stores it in `HttpContext.Items["User"]`.
  - Custom `[Authorize(...)]` attribute checks role from `HttpContext.Items["User"]`.
- `UsersController` exposes `/users/authenticate` for local username/password login.
- Roles and user metadata (`ClientFilter`, `AllowedClassifications`) come from Fig user records.
- Machine-client endpoints remain `[AllowAnonymous]` and use `clientSecret` headers (e.g., `/clients/{clientName}/settings`, `/statuses/{clientName}`, `/clients` registration).

### Web (Blazor WASM)
- `AccountService` performs `/users/authenticate`, stores token/user in local storage.
- `HttpService` attaches bearer token from local storage.
- Route guarding is done in `AppRouteView` by checking user presence + role markers.
- Main nav renders `Users` page link for administrators.

### Aspire AppHost
- AppHost currently wires API, web, and examples.
- No identity provider resource exists yet.

---

## 3) Target Architecture

Introduce a **dual-mode auth architecture** with explicit mode selection in both API and web:

- `FigManaged` mode (existing behavior)
- `Keycloak` mode (new behavior)

### 3.1 API Auth Strategy
Implement a mode-aware user-auth abstraction:

- `IUserAuthenticationModeService` (or equivalent)
  - `Mode` (`FigManaged` | `Keycloak`)
  - `TryResolveUser(HttpContext)` returns normalized `UserDataContract`-like principal model
  - `AuthenticateLocalUser(...)` enabled only in `FigManaged`

- Two implementations:
  - `FigManagedUserAuthService`
    - Reuses existing token handler + repository lookup.
  - `KeycloakUserAuthService`
    - Validates JWT using Keycloak issuer metadata/JWKS.
    - Maps claims to normalized user context:
      - username/display name
      - role(s)
      - allowed classifications
      - client filter regex

Keep machine-client secret auth untouched in services/controllers that already rely on `clientSecret` headers.

### 3.2 Authorization Strategy
Continue to enforce endpoint-level role rules, but source role from normalized user context for both modes.

Recommended implementation approach:
- Keep custom `[Authorize(Role...)]` attribute to minimize blast radius.
- Refactor auth middleware to populate `HttpContext.Items["User"]` from either:
  - Fig user repository (Fig mode), or
  - Keycloak claims mapping (Keycloak mode).

This avoids a full rewrite to ASP.NET policy attributes and keeps existing controller annotations intact.

### 3.3 Web Strategy
Introduce mode-aware authentication in web:

- `IWebAuthenticationModeService`
  - `Mode` from `WebSettings.Authentication`
  - `Login`, `Logout`, `GetCurrentUser`, `AcquireAccessToken`

Implementations:
- `FigManagedWebAuthService` (existing username/password page flow)
- `KeycloakWebAuthService` (OIDC Authorization Code + PKCE)

`AccountService` becomes facade over mode-specific implementation.

### 3.4 AppHost Strategy
Add Keycloak resource into Aspire for development and dashboard visibility.

Preferred:
- Use Aspire-native Keycloak integration package if available for your Aspire version.
Fallback:
- Add Keycloak as container resource + env vars + exposed ports + realm import volume.

---

## 4) Configuration Design

## 4.1 Shared Concepts
Introduce explicit auth mode enum in both API and web settings:

- `AuthMode`: `FigManaged`, `Keycloak`

### 4.2 API Settings (new section)
Add to `ApiSettings`:

```json
"Authentication": {
  "Mode": "FigManaged",
  "Keycloak": {
    "Authority": "https://localhost:8443/realms/fig",
    "Audience": "fig-api",
    "RequireHttpsMetadata": true,
    "RoleClaimPath": "realm_access.roles",
    "RoleClaimType": "role",
    "AllowedClassificationsClaim": "fig_allowed_classifications",
    "ClientFilterClaim": "fig_client_filter",
    "UsernameClaim": "preferred_username",
    "NameClaim": "name"
  }
}
```

Behavior:
- `Mode = FigManaged`: current token validation and `/users/authenticate` active.
- `Mode = Keycloak`: only Keycloak tokens accepted for user-protected endpoints.

### 4.3 Web Settings (new section)
Add to `WebSettings`:

```json
"Authentication": {
  "Mode": "FigManaged",
  "Keycloak": {
    "Authority": "https://localhost:8443/realms/fig",
    "ClientId": "fig-web",
    "DefaultScopes": ["openid", "profile", "email", "roles"],
    "ResponseType": "code",
    "PostLogoutRedirectUri": "/",
    "ApiScope": "fig-api"
  }
}
```

Behavior:
- `FigManaged`: existing login page and credential flow.
- `Keycloak`: automatic OIDC redirect and return.

### 4.4 Startup Validation
Add startup checks in API and web:
- If mode is `Keycloak`, required Keycloak config fields must exist.
- Log clear fatal errors on invalid config.
- Optional: expose warning endpoint/status when API and web modes mismatch.

---

## 5) Claims and Authorization Mapping

## 5.1 Role Mapping
Map Keycloak token claims to Fig roles.

Two supported patterns (configurable):
1. Realm/client roles (e.g., `realm_access.roles`) containing exact Fig role names.
2. Dedicated claim (e.g., `fig_role`) with one or many roles.

Validation rules:
- Must map to known enum values only.
- Unknown roles ignored (or fail, configurable).
- If no recognized role, deny as unauthorized.

## 5.2 AllowedClassifications Mapping
Token claim: `fig_allowed_classifications`
- Value format options:
  - JSON array string (recommended), or
  - delimited string (secondary support)
- Parse into `List<Classification>`.
- If missing/invalid:
  - Recommended default: all classifications **only if role is Administrator**, otherwise deny or empty list (see clarifications).

## 5.3 Client Filter Mapping
Token claim: `fig_client_filter`
- If missing: default to `.*`.
- If invalid regex: deny request with clear message and audit event.

## 5.4 User Identity Fields
Populate normalized user context from claims:
- `Username`: `preferred_username` fallback to `sub`
- `FirstName/LastName`: parse from claims if present; optional in Keycloak mode
- `Role`, `ClientFilter`, `AllowedClassifications` from mapped claims

---

## 6) API Behavior by Mode

## 6.1 FigManaged Mode
- No behavior change.
- `/users/authenticate` returns Fig JWT.
- `/users` CRUD remains available to administrator.

## 6.2 Keycloak Mode
- User-protected endpoints accept only Keycloak access tokens.
- `/users/authenticate`: return `404 Not Found` or `400 BadRequest` with explicit message (`Not supported in Keycloak mode`).
- `/users/register|update|delete|get`: blocked (same as above), because identity lifecycle is external.
- Existing `[Authorize(Role...)]` semantics remain unchanged via mapped claims.
- Machine-client endpoints continue unchanged:
  - `/clients` registration via `clientSecret`
  - `/clients/{client}/settings` via `clientSecret`
  - `/statuses/{client}` via `clientSecret`

## 6.3 Auditing and Events
- Continue event logging with `AuthenticatedUser` semantics.
- In Keycloak mode, record claim-derived username in event logs.

---

## 7) Web UX Behavior by Mode

## 7.1 FigManaged Mode
- Keep current `/account/login` form.
- Keep current local storage user model.
- Keep `/users` page and admin nav link.

## 7.2 Keycloak Mode
- Protected routes trigger OIDC challenge automatically.
- User is redirected to Keycloak login if no valid session.
- On success, user returns to requested Fig route (`returnUrl` preserved).
- API calls include Keycloak access token bearer.
- `Users` nav item and `/users` route are hidden/disabled.
- `Manage Account` behavior:
  - Recommended: hide password/identity update controls and show read-only identity info + link to Keycloak account management (if configured).

## 7.3 Logout
- Keycloak mode logout should:
  - clear local app state,
  - trigger OIDC sign-out / end-session endpoint,
  - return to app base URL.

---

## 8) Aspire / Local Development Design

Add Keycloak to `Fig.AppHost` with:
- Keycloak container resource
- Exposed admin and auth ports
- Realm import mounted from repo (`resources/keycloak/realm-export.json`)
- Env for admin username/password
- Dependencies:
  - API gets `Authentication:Keycloak:Authority`
  - Web gets `Authentication:Keycloak:*` settings

Also include:
- A seed realm with:
  - `fig-api` client (confidential/public depending API design)
  - `fig-web` public client for SPA + PKCE
  - roles matching Fig enum
  - protocol mappers for `fig_allowed_classifications` and `fig_client_filter`

---

## 9) Proposed Implementation Phases

### Phase 1: Config + Mode Infrastructure
- Add auth mode settings classes (API/web).
- Add startup validation and mode logging.
- Add mode abstraction interfaces.

### Phase 2: API Keycloak Validation + Claim Mapping
- Add JWT bearer validation against Keycloak authority.
- Implement claim-to-user mapping.
- Integrate into auth middleware pipeline.
- Gate `/users/*` local-management endpoints by mode.

### Phase 3: Web OIDC Flow
- Add Keycloak OIDC auth service.
- Refactor `AccountService` to mode facade.
- Route guard updates for challenge and callback handling.
- Nav/route behavior updates (`Users` page suppression in Keycloak mode).

### Phase 4: AppHost + Dev Realm
- Add Keycloak resource/container and config wiring.
- Add realm export/import assets and docs.

### Phase 5: Hardening + Tests
- Add/adjust integration tests for both modes.
- Add regression tests for machine-client endpoints.
- Add role/claim mapping tests and invalid claim cases.

---

## 10) Testing Strategy

## 10.1 API Tests
- Fig mode regression: all existing auth tests pass unchanged.
- Keycloak mode:
  - valid token with each role can access expected endpoints,
  - missing role claim denied,
  - bad `ClientFilter` claim denied,
  - missing/invalid `AllowedClassifications` handling verified,
  - `/users/authenticate` and user management routes blocked.
- Machine-client regression:
  - `/clients` registration by client secret still works,
  - `/clients/{name}/settings` still works,
  - `/statuses/{name}` still works.

## 10.2 Web Tests
- Fig mode login/logout unchanged.
- Keycloak mode:
  - first navigation redirects to Keycloak,
  - callback returns to original route,
  - API calls include bearer token,
  - `/users` nav item absent,
  - unauthorized role cannot access admin pages.

## 10.3 End-to-End
- AppHost run with Keycloak enabled validates full loop:
  - web -> keycloak login -> back to web -> API authorized operations.

---

## 11) Backward Compatibility and Rollout

- Default auth mode remains `FigManaged` to avoid breaking existing deployments.
- Keycloak support is opt-in via config.
- Existing user data remains intact and reusable if switching back to Fig mode.
- Add migration/runbook docs:
  - how to configure Keycloak clients/claims,
  - how to flip mode safely,
  - smoke-test checklist.

---

## 12) Risks and Mitigations

- **Risk:** Claim schema drift in Keycloak realm.
  - **Mitigation:** Configurable claim names/paths + startup self-check endpoint.
- **Risk:** Mode mismatch between web and API.
  - **Mitigation:** Startup warning + health/status indicator.
- **Risk:** Breaking machine-client flows.
  - **Mitigation:** Explicitly preserve `[AllowAnonymous]` + client-secret tests.
- **Risk:** Token parsing inconsistencies (array vs string claims).
  - **Mitigation:** robust parser with strict validation + diagnostics.

---

## 13) Clarification Questions (with recommendations)

1. **Should Keycloak mode support only realm roles, or also client roles?**
   - **Recommendation:** Support both, configurable claim path, default to realm roles.

Use recommendation.

2. **What is the fallback when `fig_allowed_classifications` is absent?**
   - **Recommendation:**
     - `Administrator`: default to all classifications.
     - non-admin roles: deny access (secure-by-default).

Use recommendation.

3. **Should `/account/manage` remain editable in Keycloak mode?**
   - **Recommendation:** No. Make it read-only and redirect identity edits to Keycloak account page.

Use recommendation.

4. **How should `/users/authenticate` behave in Keycloak mode?**
   - **Recommendation:** Return `404` to clearly signal endpoint not available in this mode.

Use recommendation.

5. **Should API accept both Fig JWT and Keycloak JWT during migration windows?**
   - **Recommendation:** No for steady-state security. If needed, add a short-lived explicit `Dual` migration mode guarded by feature flag and expiry date.

No, it will only accept one or the other but not both at the same time.

1. **Where should `AllowedClassifications` and `ClientFilter` live in Keycloak?**
   - **Recommendation:** User attributes mapped to token claims via protocol mappers; avoid deriving from groups unless already standardized.

Use recommendation. Ensure that this is well documented.

2. **Do service accounts in Keycloak need API user-role access?**
   - **Recommendation:** Keep machine clients on existing client-secret flow; do not expand Keycloak scope for them in this phase.

Use recommendation.

---


## 14) Recommended Path Forward

- Implement dual-mode with minimal surface change by preserving existing custom authorization attributes and injecting a mode-aware principal resolver.
- Keep current Fig mode untouched and fully backward-compatible.
- Add Keycloak OIDC flow to web and Keycloak JWT validation to API under explicit config.
- Preserve machine-client secret auth exactly as-is.
- Introduce Keycloak resource in Aspire with preconfigured realm export for fast developer onboarding.
