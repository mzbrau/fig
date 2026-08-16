# Fig End-to-End Tests

Self-sufficient Playwright UI tests that start Fig via Aspire (`Fig.E2E.AppHost`): API, Web, AspNetApi, and DisplayScriptExample.

## Run locally

Ports `7148` and `7281` must be free.

```bash
dotnet build src/tests/Fig.EndToEnd.Tests/Fig.EndToEnd.Tests.csproj -c Release
pwsh src/tests/Fig.EndToEnd.Tests/bin/Release/net10.0/playwright.ps1 install chromium
dotnet test src/tests/Fig.EndToEnd.Tests/Fig.EndToEnd.Tests.csproj -c Release --filter Category=E2E
```

Optional: point at an already-running stack (skips Aspire startup):

```bash
export FIG_E2E_WEB_URL=https://localhost:7148
dotnet test src/tests/Fig.EndToEnd.Tests/Fig.EndToEnd.Tests.csproj -c Release --filter Category=E2E
```

These tests are excluded from the default release `dotnet test ./src` filter (`FullyQualifiedName!~Fig.EndToEnd.Tests`).
