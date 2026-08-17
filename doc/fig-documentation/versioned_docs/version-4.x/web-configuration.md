---
sidebar_position: 10
---

# Web Configuration

Fig Web is a Blazor WebAssembly application. Configuration lives in `wwwroot/appsettings.json` (or is substituted at container start via `FIG_API_URI` — see the Web Docker entrypoint).

## `WebSettings`

| Setting | Description | Default |
| ------- | ----------- | ------- |
| `ApiUri` | Fig API address the browser calls. In Docker, set `FIG_API_URI`; the entrypoint writes it into `appsettings.json`. | `https://localhost:7281` |
| `Environment` | Label shown in the UI (for example in the change dialog). | `Development` |
| `DefaultDisplayCollapsed` | When `true`, the settings page opens in [compact view](./features/17-compact-view.md). | `true` |
| `Authentication` | `FigManaged` (default) or `Keycloak`. Must match the API. See [Security](./security.md#keycloak-authentication-mode). | `FigManaged` |

Example:

```json
{
  "WebSettings": {
    "ApiUri": "https://localhost:7281",
    "Environment": "Development",
    "DefaultDisplayCollapsed": true,
    "Authentication": {
      "Mode": "FigManaged"
    }
  }
}
```

Keycloak fields (`Authority`, `ClientId`, `Scopes`, `ApiScope`, role mappings including **Dashboard**, and so on) are documented under [Security — Web configuration](./security.md#web-configuration).
