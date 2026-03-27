---
sidebar_position: 2
---

# Fig MCP Server

The Fig MCP (Model Context Protocol) Server enables AI tools such as GitHub Copilot CLI, Claude Desktop, Cursor, and other MCP-compatible clients to interact with your Fig configuration management system.

:::warning[Experimental]

This integration is experimental and may not work as expected. Create a ticket on GitHub if a problem is found.

:::

## Features

- **Configuration Analysis**: Ask AI to review settings across all clients, identify patterns, and suggest improvements
- **Anomaly Detection**: Let AI spot unusual values, inconsistencies, or misconfigured settings
- **Event Log Queries**: Query event logs and setting change timelines through natural language
- **Session Monitoring**: View active client run sessions with health, memory, and version info
- **Setting History**: Review the value change history of any setting
- **Operational Management** (opt-in): Update setting values, manage webhooks, lookup tables, users, and more
- **Configurable Safety**: Each operation category can be independently enabled or disabled — read-only by default

## Architecture

Fig.Mcp is a standalone ASP.NET Core application that proxies requests to the Fig API over HTTP. It does not connect to the database directly. This design provides:

- **Deployment independence** — deploy and scale separately from the API
- **Isolated attack surface** — the MCP server runs in its own process
- **Simple operation gating** — disabled tools are simply not registered, so the AI never sees them
- **Easy testing** — mock HTTP responses without needing a full API bootstrap

```
┌──────────────┐     MCP Protocol      ┌──────────────┐    REST/HTTP     ┌──────────────┐
│   AI Tool    │ ◄──────────────────► │   Fig.Mcp    │ ◄──────────────► │   Fig.Api    │
│ (Copilot,    │   (stdio or HTTP)     │  MCP Server  │   (JSON + JWT)   │              │
│  Claude,     │                       │              │                   │              │
│  Cursor)     │                       └──────────────┘                   └──────────────┘
└──────────────┘
```

## Transport Modes

Fig.Mcp supports two MCP transport modes:

| Mode | Use Case | How It Works |
|------|----------|-------------|
| **stdio** (default) | Local AI tools (Copilot CLI, Claude Desktop, Cursor) | AI tool launches `fig-mcp` as a subprocess |
| **HTTP** | Remote/server-side AI agents, hosted tool platforms | Runs as an HTTP server with Streamable HTTP + SSE |

## Configuration

All settings are configured via `appsettings.json` or environment variables.

### Basic Settings

```json
{
  "McpSettings": {
    "FigApiBaseUrl": "https://localhost:7281",
    "Username": "admin",
    "Password": "your-password",
    "Transport": "stdio"
  }
}
```

| Setting | Description | Default |
|---------|-------------|---------|
| `FigApiBaseUrl` | URL of the Fig API | `https://localhost:7281` |
| `Username` | Fig user to authenticate as | `admin` |
| `Password` | Password for the Fig user | _(empty)_ |
| `Transport` | `stdio` or `http` | `stdio` |

Environment variable override format: `McpSettings__FigApiBaseUrl`, `McpSettings__Password`, etc.

### Tool Gates

Each tool category can be independently enabled or disabled. **By default, all reads are enabled and all writes are disabled.**

```json
{
  "McpSettings": {
    "ToolGates": {
      "ReadSettings": true,
      "WriteSettings": false,
      "ReadEvents": true,
      "ReadSessions": true,
      "ReadHistory": true,
      "ManageClients": false,
      "DeleteClients": false,
      "ManageUsers": false,
      "ManageWebHooks": false,
      "ManageLookupTables": false,
      "ManageScheduling": false,
      "ManageTimeMachine": false,
      "ExecuteCustomActions": false,
      "ImportExportData": false,
      "ManageConfiguration": false
    }
  }
}
```

Disabled tools are not registered with the MCP server — the AI never sees them and cannot invoke them.

## Setup

### GitHub Copilot CLI

Add to `.github/copilot/mcp.json` in your repository:

```json
{
  "servers": {
    "fig": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/mcp/Fig.Mcp"],
      "env": {
        "McpSettings__FigApiBaseUrl": "https://localhost:7281",
        "McpSettings__Username": "admin",
        "McpSettings__Password": "your-password"
      }
    }
  }
}
```

### Claude Desktop

Add to your `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "fig": {
      "command": "dotnet",
      "args": ["run", "--project", "/path/to/src/mcp/Fig.Mcp"],
      "env": {
        "McpSettings__FigApiBaseUrl": "https://localhost:7281",
        "McpSettings__Username": "admin",
        "McpSettings__Password": "your-password"
      }
    }
  }
}
```

### HTTP Mode (Remote Agents)

Run the MCP server as an HTTP service:

```bash
McpSettings__Transport=http dotnet run --project src/mcp/Fig.Mcp -- --urls http://localhost:3001
```

### Docker

```bash
docker run -e McpSettings__FigApiBaseUrl=http://fig-api:8080 \
           -e McpSettings__Username=admin \
           -e McpSettings__Password=your-password \
           -e McpSettings__Transport=http \
           -p 3001:8080 \
           mzbrau/fig-mcp:latest
```

Or using docker-compose (see `docker-compose.yml` in the repository root).

## Available Tools

### Read-Only Tools (enabled by default)

| Tool | Gate | Description |
|------|------|-------------|
| `ListClients` | ReadSettings | List all registered clients with settings |
| `GetClientDescriptions` | ReadSettings | Lightweight client name + description list |
| `GetEvents` | ReadEvents | Query event logs by time range |
| `GetEventCount` | ReadEvents | Total event log count |
| `GetClientTimeline` | ReadEvents | Setting change timeline for a client |
| `GetSettingHistory` | ReadHistory | Value history for a specific setting |
| `GetLastChanged` | ReadHistory | Last-changed timestamps across all clients |
| `GetRunSessions` | ReadSessions | Active client sessions with health info |
| `GetApiVersion` | ReadSettings | Fig API version |
| `GetApiStatus` | ReadSettings | API health status |
| `ListLookupTables` | ReadSettings | All lookup tables |
| `ListWebHooks` | ReadSettings | All webhooks |
| `ListWebHookClients` | ReadSettings | Webhook clients |
| `ListCheckPoints` | ReadSettings | Time machine checkpoints |
| `GetCheckPointData` | ReadSettings | Checkpoint payload |
| `ListDeferredChanges` | ReadSettings | Scheduled setting changes |
| `GetCustomActionStatus` | ReadSettings | Custom action execution status |
| `GetCustomActionHistory` | ReadSettings | Custom action execution history |
| `GetDeferredImports` | ReadSettings | Pending deferred imports |
| `GetClientRegistrationHistory` | ReadSettings | Client registration history |

### Write Tools (disabled by default)

| Tool | Gate | Description |
|------|------|-------------|
| `UpdateSettingValues` | WriteSettings | Update setting values for a client |
| `ToggleLiveReload` | WriteSettings | Enable/disable live reload for a session |
| `RequestClientRestart` | WriteSettings | Request a client restart |
| `ChangeClientSecret` | ManageClients | Rotate a client's secret |
| `DeleteClient` | DeleteClients | Delete a client |
| `CreateLookupTable` | ManageLookupTables | Create a lookup table |
| `UpdateLookupTable` | ManageLookupTables | Update a lookup table |
| `DeleteLookupTable` | ManageLookupTables | Delete a lookup table |
| `CreateWebHook` | ManageWebHooks | Create a webhook |
| `UpdateWebHook` | ManageWebHooks | Update a webhook |
| `DeleteWebHook` | ManageWebHooks | Delete a webhook |
| `TestWebHookClient` | ManageWebHooks | Test webhook connectivity |
| `ApplyCheckPoint` | ManageTimeMachine | Restore to a checkpoint |
| `UpdateCheckPointNote` | ManageTimeMachine | Add note to checkpoint |
| `RescheduleDeferredChange` | ManageScheduling | Reschedule a deferred change |
| `DeleteDeferredChange` | ManageScheduling | Cancel a scheduled change |
| `ExecuteCustomAction` | ExecuteCustomActions | Execute a custom action |
| `ListUsers` / `GetUser` / `CreateUser` / `UpdateUser` / `DeleteUser` | ManageUsers | User management |
| `ExportData` / `ImportData` / `ExportValuesOnly` / `ImportValuesOnly` | ImportExportData | Data import/export |
| `GetConfiguration` / `UpdateConfiguration` | ManageConfiguration | API configuration |

## Example AI Interactions

Once connected, you can ask your AI tool questions like:

- *"List all registered Fig clients and summarize their settings"*
- *"Are there any settings with empty or default descriptions that should be documented?"*
- *"Show me the event log for the last 24 hours — are there any unusual patterns?"*
- *"Which clients have the most settings? Are any over-configured?"*
- *"Compare the settings between the production and staging instances of ServiceX"*
- *"Show me the value history for the DatabaseConnectionTimeout setting"*
- *"Which clients haven't reported a run session recently?"*

## Security Considerations

- The MCP server authenticates to the Fig API using a pre-configured user's credentials
- It inherits the permissions of that user — a ReadOnly user cannot perform write operations
- Tool gates provide an additional layer of protection at the MCP level
- For production, use environment variables or secret providers for credentials — never commit passwords
- In HTTP transport mode, consider network-level access controls (firewall, VPN, etc.)
