# Agents Guide

## Solution overview
- Fig is a .NET 10 microservices configuration management solution.
- Projects live under src/ (API, web, client, common, hosting, integrations, tools, tests).
- Examples and documentation live under examples/ and doc/.

## JSON serialization guidance
- Prefer Newtonsoft.Json over System.Text.Json throughout the solution.
- Use shared settings from Fig.Common.NetStandard.Json (e.g., JsonSettings) when applicable to keep compatibility with existing contracts and the .NET Standard client.
- Avoid introducing System.Text.Json usage in new code.
- **TypeNameHandling.Objects is required** for API controllers (default), `JsonSettings.FigDefault` / `FigHttp`, and Fig.Client HTTP. Do **not** use `TypeNameHandling.Auto` on shared or HTTP settings — Auto emits `$type` for LINQ iterators assigned to `IEnumerable` properties (e.g. deferred changes) and breaks clients.
- **Exception — `GET /clients` only:** use [`FigWebLoadJsonSettings`](src/common/Fig.Contracts/Json/FigWebLoadJsonSettings.cs) (`TypeNameHandling.None` + [`SettingValueCompactJsonConverter`](src/common/Fig.Contracts/Json/SettingValueCompactJsonConverter.cs)) for Fig.Web / Fig.Mcp load. Polymorphic `SettingValueBaseDataContract` values use short `t`/`v` discriminators instead of `$type`. Do not apply this dialect to Fig.Client register/get-settings.
- Keep `FigSerializationBinder` allow-listing for TypeNameHandling deserialize paths.

## Notes for changes
- API Newtonsoft settings must stay consistent with the client and shared contracts (`TypeNameHandling.Objects` + binder), except the documented `GET /clients` FigWebLoad path.
- For user-provided JSON, use safe JsonSerializerSettings (`TypeNameHandling.None` or whitelisted binders as needed).

## Testing Strategy

Fig uses a layered testing strategy. Different types of tests exist for different purposes, and they should complement rather than replace one another. When adding tests, think about *why* a test belongs at a particular level instead of adding tests indiscriminately.

### API Tests (Highest Priority)

The API is the primary integration point for the application.

Whenever functionality is added or modified, comprehensive API tests should normally be added. These tests should exercise behaviour through the public API and provide confidence that the system continues to function correctly after changes.

API tests should verify:

- successful (happy path) behaviour
- validation
- authorisation/security
- error handling
- business rules
- persistence
- interactions between components

The API test suite should be considered the main regression safety net for the product.

### Unit Tests

Unit tests remain essential even when API tests exist.

They should focus on:

- business logic in isolation
- algorithms
- edge cases
- null handling
- invalid inputs
- exceptional conditions
- behaviour that would be difficult or cumbersome to exercise through API tests

Unit tests should execute quickly and provide precise failure information.

They are especially important in the Client and Web projects where there is less integration-style coverage than the API.

Developers should generally add unit tests whenever introducing non-trivial logic.

### Client and Web Testing

The Client and Web applications should have good unit test coverage.

Since these projects are not exercised as comprehensively through API tests, unit tests become more important for:

- UI state management
- view models
- client-side services
- JavaScript interop wrappers
- helper classes
- data transformation logic

Avoid relying solely on manual testing for client-side behaviour.

### End-to-End (Playwright) Tests

Playwright tests exist to verify that complete user workflows function correctly.

These tests should intentionally remain relatively small in number because they are:

- slower
- more expensive to execute
- potentially more brittle than lower-level tests

Use Playwright to validate:

- critical user journeys
- application startup
- authentication
- navigation
- integration between major components
- browser-specific behaviour

Avoid using Playwright to exhaustively test business rules that can be covered more reliably by API or unit tests.

### Choosing the Right Test

When implementing a feature, think in terms of the testing pyramid.

Typically:

- add or update API tests for externally observable behaviour
- add unit tests for new logic and edge cases
- add Playwright tests only if the change affects an important end-to-end workflow

Most features should result in API tests and unit tests. Only user-visible workflows generally require Playwright coverage.

### General Principles

- Tests should be deterministic and independent.
- Tests should avoid unnecessary duplication while providing confidence at the appropriate level.
- Fast-running tests are preferred where they provide equivalent confidence.
- Every bug fix should ideally include a regression test at the most appropriate level.
- When deciding where a test belongs, favour the lowest level that can effectively verify the behaviour.
