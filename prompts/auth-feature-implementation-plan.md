# Auth Feature Implementation Plan: Keycloak + FigManaged Dual Mode

This plan converts the approved design into an execution-ready sequence for a coding agent, mapped to the current Fig codebase.

## 1) Confirmed Decisions (from design clarifications)

- Support both realm roles and client roles in Keycloak (configurable claim path).
- In Keycloak mode, if `fig_allowed_classifications` is missing:
  - `Administrator` => all classifications
  - non-admin => deny access.
- `/account/manage` in Keycloak mode is read-only for Fig profile fields and should link out to Keycloak account management if configured.
- `/users/authenticate` in Keycloak mode returns `404`.
- API accepts only one user token system at a time (`FigManaged` or `Keycloak`), never both simultaneously.
- `AllowedClassifications` and `ClientFilter` come from Keycloak user attributes mapped to token claims in Keycloak mode.
- Machine clients remain on existing `clientSecret` flow (no Keycloak service-account scope in this phase).

## 2) Current Code Anchors (for implementation)

- API startup + DI + middleware: `src/api/Fig.Api/Program.cs`
- API settings model: `src/api/Fig.Api/ApiSettings.cs`
- Existing token validation: `src/api/Fig.Api/Authorization/ITokenHandler.cs`, `src/api/Fig.Api/Authorization/TokenHandler.cs`
- Existing user auth middleware: `src/api/Fig.Api/Middleware/AuthMiddleware.cs`
- Existing role enforcement: `src/api/Fig.Api/Attributes/AuthorizeAttribute.cs`
- User endpoints to gate in Keycloak mode: `src/api/Fig.Api/Controllers/UsersController.cs`
- Existing machine-client endpoints that must stay unchanged:
  - `src/api/Fig.Api/Controllers/ClientsController.cs`
  - `src/api/Fig.Api/Controllers/StatusController.cs`
  - `src/api/Fig.Api/Controllers/LookupTablesController.cs`

- Web startup + DI: `src/web/Fig.Web/Program.cs`
- Web settings model: `src/web/Fig.Web/WebSettings.cs`
- Existing web auth service: `src/web/Fig.Web/Services/IAccountService.cs`, `src/web/Fig.Web/Services/AccountService.cs`
- Existing bearer attachment: `src/web/Fig.Web/Services/HttpService.cs`
- Existing route guard: `src/web/Fig.Web/Routing/AppRouteView.cs`
- Login/manage/logout pages:
  - `src/web/Fig.Web/Pages/Login.razor`
  - `src/web/Fig.Web/Pages/Login.razor.cs`
  - `src/web/Fig.Web/Pages/ManageAccount.razor`
  - `src/web/Fig.Web/Pages/ManageAccount.razor.cs`
  - `src/web/Fig.Web/Pages/Logout.razor`
- Users UI + nav entry to suppress in Keycloak mode:
  - `src/web/Fig.Web/Pages/Users.razor`
  - `src/web/Fig.Web/Pages/Users.razor.cs`
  - `src/web/Fig.Web/Shared/MainLayout.razor`

- Aspire host: `src/hosting/Fig.AppHost/Program.cs`

- Existing integration test base/helpers:
  - `src/tests/Fig.Test.Common/IntegrationTestBase.cs`
  - `src/tests/Fig.Integration.Test/Api/UserIntegrationTests.cs`

## 3) Execution Order (agent-ready)

### Progress tracker

- [x] Step 0 - Branch hygiene and baseline validation
- [x] Step 1 - Introduce mode-aware auth configuration models
- [x] Step 2 - API auth mode abstraction and principal normalization
- [x] Step 3 - API middleware integration (no endpoint annotation rewrite)
- [x] Step 4 - API startup wiring for Keycloak mode
- [x] Step 5 - Gate user lifecycle endpoints in Keycloak mode
- [x] Step 6 - Web dual-mode auth abstraction
- [ ] Step 7 - Web route, nav, and account UX behavior in Keycloak mode
- [ ] Step 8 - Token propagation in web HTTP client
- [ ] Step 9 - Aspire AppHost Keycloak resource integration
- [ ] Step 10 - Tests for both modes and machine-client regression
- [ ] Step 11 - Documentation and runbook updates
- [ ] Step 12 - Full validation sequence (must run before merge)

## Step 0 - Branch hygiene and baseline validation

1. Ensure clean working tree and restore/build baseline:
   - `cd src`
   - `dotnet restore Fig.sln`
   - `dotnet build Fig.sln --configuration Release --no-restore`
2. Run fast baseline tests:
   - `dotnet test tests/Fig.Unit.Test/Fig.Unit.Test.csproj --configuration Release --no-build --verbosity minimal`

**Exit criteria**
- Baseline passes before feature changes begin.

---

## Step 1 - Introduce mode-aware auth configuration models

### API
1. Extend `ApiSettings` with:
   - `Authentication` object
   - `Mode` enum/string (`FigManaged`, `Keycloak`)
   - nested `Keycloak` settings object (`Authority`, `Audience`, `RequireHttpsMetadata`, role + claim mapping fields).
2. Keep existing `Secret`/`TokenLifeMinutes` unchanged for FigManaged compatibility.

### Web
3. Extend `WebSettings` with:
   - `Authentication` object
   - `Mode` (`FigManaged`, `Keycloak`)
   - nested Keycloak SPA settings (`Authority`, `ClientId`, scopes, `ResponseType`, `PostLogoutRedirectUri`, `ApiScope`, optional account-management URL).

### Config files
4. Add new sections to:
   - `src/api/Fig.Api/appsettings.json`
   - `src/web/Fig.Web/wwwroot/appsettings.json`
   using `FigManaged` defaults.

**Exit criteria**
- App starts in FigManaged with no behavior change.
- Missing required Keycloak fields cause explicit startup validation errors only when mode=`Keycloak`.

---

## Step 2 - API auth mode abstraction and principal normalization

1. Add shared API auth-mode abstractions (new folder recommended `src/api/Fig.Api/Authorization/UserAuth/`):
   - `AuthMode` enum
   - `IUserAuthenticationModeService`
   - `NormalizedUserContext` (maps to what `AuthorizeAttribute` + services need)
   - supporting options/validation types.
2. Implement `FigManagedUserAuthenticationModeService`:
   - reuses existing `ITokenHandler.Validate(...)`
   - resolves user via `IUserService.GetById(...)`
   - preserves existing `HttpContext.Items["User"]` semantics.
3. Implement `KeycloakUserAuthenticationModeService`:
   - validates bearer token against Keycloak authority/JWKS
   - maps username/name/role/classifications/client filter from claims
   - enforces clarified fallback rules.

**Important**
- Keep Newtonsoft usage conventions; do not introduce `System.Text.Json` for application contract serialization.

**Exit criteria**
- A single service can resolve authenticated user context for either mode.

---

## Step 3 - API middleware integration (no endpoint annotation rewrite)

1. Refactor `AuthMiddleware` (`src/api/Fig.Api/Middleware/AuthMiddleware.cs`) to use `IUserAuthenticationModeService`.
2. Continue setting `HttpContext.Items["User"]` so existing `AuthorizeAttribute` keeps working.
3. Continue calling `SetAuthenticatedUser(...)` on `IAuthenticatedService` instances.
4. Ensure unauthenticated requests remain allowed where controller actions use `[AllowAnonymous]`.

**Exit criteria**
- Existing `[Authorize(Role...)]` behavior is unchanged in FigManaged.
- Keycloak mode injects claim-derived user context for same authorization path.

---

## Step 4 - API startup wiring for Keycloak mode

1. In `src/api/Fig.Api/Program.cs`:
   - register auth-mode services and validators.
   - configure JWT validation dependencies for Keycloak mode.
   - add startup mode logging (include selected auth mode + critical config values excluding secrets).
2. Add a startup validator service (new file) that fails fast on invalid Keycloak config.

**Exit criteria**
- App fails clearly on invalid Keycloak config and starts cleanly on valid config.

---

## Step 5 - Gate user lifecycle endpoints in Keycloak mode

1. Update `src/api/Fig.Api/Controllers/UsersController.cs`:
   - `POST /users/authenticate` returns `404` in Keycloak mode.
   - user CRUD endpoints (`register`, `get`, `get/{id}`, `update`, `delete`) return `404` in Keycloak mode.
2. Preserve full existing behavior in FigManaged mode.

**Exit criteria**
- User lifecycle is externally managed in Keycloak mode with explicit endpoint unavailability.

---

## Step 6 - Web dual-mode auth abstraction

1. Introduce web-mode abstractions (recommended folder `src/web/Fig.Web/Services/Authentication/`):
   - `IWebAuthenticationModeService`
   - `FigManagedWebAuthenticationModeService`
   - `KeycloakWebAuthenticationModeService`.
2. Keep existing `IAccountService` as facade contract to minimize blast radius.
3. Refactor `AccountService` to delegate to selected mode service.

**Key implementation detail**
- In Keycloak mode, do not use `/users/authenticate`.
- Access token acquisition must use OIDC Authorization Code + PKCE flow.

**Likely package update**
- Add `Microsoft.AspNetCore.Components.WebAssembly.Authentication` to `src/web/Fig.Web/Fig.Web.csproj` if not already present.

**Exit criteria**
- `IAccountService` callers remain intact while auth behavior switches by config mode.

---

## Step 7 - Web route, nav, and account UX behavior in Keycloak mode

1. Update route guard logic in `src/web/Fig.Web/Routing/AppRouteView.cs`:
   - protected routes trigger OIDC challenge/redirect in Keycloak mode.
   - preserve return URL handling.
2. Update login/logout pages:
   - `Login` page should redirect/challenge in Keycloak mode instead of local form submit flow.
   - `Logout` should clear local state and invoke OIDC end-session flow.
3. Update manage account page:
   - `src/web/Fig.Web/Pages/ManageAccount.razor`
   - `src/web/Fig.Web/Pages/ManageAccount.razor.cs`
   - make Fig identity fields read-only in Keycloak mode and optionally show link to Keycloak account management.
4. Hide/suppress users management in Keycloak mode:
   - nav link in `src/web/Fig.Web/Shared/MainLayout.razor`
   - route/page availability for `src/web/Fig.Web/Pages/Users.razor`.

**Exit criteria**
- Keycloak mode has redirect-based login and no editable Fig user-management UX.

---

## Step 8 - Token propagation in web HTTP client

1. Update `src/web/Fig.Web/Services/HttpService.cs` so bearer token retrieval is mode-aware:
   - FigManaged: existing local-storage token behavior.
   - Keycloak: acquire current access token from OIDC auth service and attach to API calls.
2. Ensure 401 handling remains deterministic and mode-safe.

**Exit criteria**
- API calls carry the correct token type for active mode.

---

## Step 9 - Aspire AppHost Keycloak resource integration

1. Update `src/hosting/Fig.AppHost/Program.cs` to add Keycloak resource:
   - Prefer Aspire-native Keycloak resource APIs for current Aspire version.
   - Fallback to container resource with explicit image/ports/env/volume mount.
2. Add realm import artifact under repository resources (recommended):
   - `resources/keycloak/realm-export.json`
3. Wire env vars/config for API and web Keycloak settings from AppHost.

**Exit criteria**
- `Fig.AppHost` starts API + web + Keycloak with importable/dev realm.

---

## Step 10 - Tests for both modes and machine-client regression

### Add/Update Integration Tests
1. Extend test config support (`src/tests/Fig.Test.Common/IntegrationTestBase.cs`) to toggle auth mode and inject test JWT behavior.
2. Add Keycloak-mode API tests (new test file recommended under `src/tests/Fig.Integration.Test/Api/`):
   - valid token per role => expected access.
   - unknown/no role => unauthorized.
   - invalid `fig_client_filter` regex => denied.
   - missing/invalid `fig_allowed_classifications` => fallback behavior per clarified rules.
   - `/users/authenticate` + user management endpoints => `404`.
3. Preserve existing FigManaged tests (e.g. `UserIntegrationTests`) as regression suite.
4. Add explicit machine-client regression tests for:
   - `POST /clients` (`clientSecret`)
   - `GET /clients/{clientName}/settings` (`clientSecret`)
   - `PUT /statuses/{clientName}` (`clientSecret`)

### Web behavior tests
5. If current end-to-end tests are minimal, add focused integration tests for mode-based nav suppression and route handling in web-layer tests where feasible.

**Exit criteria**
- Both auth modes are covered, and machine-client flows prove unchanged.

---

## Step 11 - Documentation and runbook updates

1. Add/extend docs with Keycloak setup + claim mapping:
   - where to configure role claim path(s)
   - how to map `fig_allowed_classifications`
   - how to map `fig_client_filter`
   - mode switching and rollback to FigManaged
2. Document that Keycloak claims should come from user attributes + protocol mappers.
3. Include troubleshooting for mode mismatch between web and API.

**Suggested doc locations**
- `README.md` (high-level)
- `doc/` (detailed setup/runbook)

**Exit criteria**
- A developer can configure Keycloak mode without reading source code.

---

## Step 12 - Full validation sequence (must run before merge)

1. Build and unit tests:
   - `cd src`
   - `dotnet restore Fig.sln`
   - `dotnet build Fig.sln --configuration Release --no-restore`
   - `dotnet test tests/Fig.Unit.Test/Fig.Unit.Test.csproj --configuration Release --no-build --verbosity minimal`
2. Integration tests:
   - `dotnet test tests/Fig.Integration.Test/Fig.Integration.Test.csproj --configuration Release --no-build --verbosity minimal`
3. Manual runtime smoke (both modes):
   - Start API + web in FigManaged mode, verify local login and `/users` admin UX.
   - Start through AppHost with Keycloak mode, verify redirect login, role-based access, hidden users UI, and machine-client secret flows.

**Exit criteria**
- No regressions in FigManaged mode.
- Keycloak mode fully operational and aligned with clarified decisions.

## 4) Delivery slices for PRs (recommended)

- PR 1: config models + API mode abstractions + middleware integration (no web).
- PR 2: web dual-mode + OIDC flow + UX gating.
- PR 3: AppHost Keycloak + realm assets + documentation.
- PR 4: tests/hardening and any bug fixes from validation.

This sequence keeps risk low, enables targeted reviews, and protects existing FigManaged behavior while adding Keycloak support incrementally.
