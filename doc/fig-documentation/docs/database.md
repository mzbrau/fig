---
sidebar_position: 8
---

# Database

Fig API supports three databases:

| Provider | Typical use |
|----------|-------------|
| **SQLite** | Default for local development and small deployments |
| **SQL Server** | **Recommended for production** (compose, Aspire, and the Windows install script) |
| **PostgreSQL** | Supported alternative for production when you prefer Postgres |

Fig creates and migrates its own schema on startup (NHibernate `SchemaUpdate` plus numbered data migrations). Setting **values** are encrypted at rest with the API secret; protect that secret as described in [API Secret Migration](./guides/1-api-secret-migration.md).

Connection string sources (in order):

1. `ConnectionStrings:Fig` (for example Aspire `WithReference(..., "Fig")`)
2. `ApiSettings:DbConnectionString` / `ApiSettings__DbConnectionString`

Optional override when the connection string is ambiguous: `ApiSettings:DatabaseProvider` = `Sqlite` | `SqlServer` | `PostgreSql`.

## SQLite (default)

```json
"ApiSettings": {
  "DbConnectionString": "Data Source=fig.db;Version=3;New=True;Busy Timeout=5000"
}
```

This is appropriate for local development and small deployments. Fig.Api uses System.Data.SQLite 2.x with native binaries, including Apple Silicon (`osx-arm64`). No extra interop libraries are required:

```
dotnet build
dotnet run --project src/api/Fig.Api
```

Or use the Aspire AppHost under `src/hosting/Fig.AppHost`.

## SQL Server (recommended for production)

Set `ApiSettings.DbConnectionString` (or `ApiSettings__DbConnectionString`) to a SQL Server connection string. Non-production servers often need `TrustServerCertificate=True`.

```
Server=myServer;User Id=fig_login;Password=...;Initial Catalog=fig;TrustServerCertificate=True
```

The repository [docker-compose.yml](https://github.com/mzbrau/fig/blob/main/docker-compose.yml) provisions SQL Server via `.env` (`fqdn`, `FIG_DB_NAME`, `FIG_USER_NAME`, `FIG_DB_PWD`, `SA_PWD`). See also [`.env.example`](https://github.com/mzbrau/fig/blob/main/.env.example).

Aspire can inject the connection string as `Fig` — see [.NET Aspire Integration](./guides/9-aspire-integration.md).

## PostgreSQL

Set an Npgsql-style connection string. Fig detects PostgreSQL from `Host=`, `Username=`, `Port=5432`, or the word `postgres` in the string. You can also set `ApiSettings:DatabaseProvider` to `PostgreSql`.

```
Host=myServer;Port=5432;Database=fig;Username=fig_login;Password=...
```

```json
"ApiSettings": {
  "DbConnectionString": "Host=myServer;Port=5432;Database=fig;Username=fig_login;Password=...",
  "DatabaseProvider": "PostgreSql"
}
```

### Docker Compose (PostgreSQL)

1. Copy [`.env.postgres.example`](https://github.com/mzbrau/fig/blob/main/.env.postgres.example) to `.env.postgres` and set `POSTGRES_PASSWORD`, `FIG_DB_PWD`, and related values.
2. From the repository root:

```bash
docker compose -f docker-compose.postgres.yml --env-file .env.postgres up
```

That stack starts Postgres, runs [`scripts/setup_fig_db_postgres.sh`](https://github.com/mzbrau/fig/blob/main/scripts/setup_fig_db_postgres.sh) to create the Fig role and database, then starts Fig API, Web, and MCP. Fig Web is at `http://localhost:7148` and the API at `http://localhost:7281`.

### Aspire

```json
{
  "ConnectionStrings": {
    "FigDb": "Host=myServer;Port=5432;Database=fig;Username=fig_login;Password=..."
  }
}
```

```csharp
var figDb = builder.AddConnectionString("FigDb");

var figApi = builder.AddFigApi("fig-api")
    .WithReference(figDb, "Fig");
```

## Schema and backups

Fig creates and migrates its own schema on startup. Back up the SQLite file, or take SQL Server / PostgreSQL backups using your usual process. Setting **values** are encrypted at rest with the API secret; protect that secret as described in [API Secret Migration](./guides/1-api-secret-migration.md).
