---
sidebar_position: 8
---

# Database

Fig API uses **SQLite** by default (embedded file database). For production, use **SQL Server**. Any SQL database supported by NHibernate can work; SQL Server is the path used in compose, Aspire, and the install script.

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

## SQL Server

Set `ApiSettings.DbConnectionString` (or `ApiSettings__DbConnectionString`) to a SQL Server connection string. Non-production servers often need `TrustServerCertificate=True`.

```
Server=myServer;User Id=fig_login;Password=...;Initial Catalog=fig;TrustServerCertificate=True
```

The repository [docker-compose.yml](https://github.com/mzbrau/fig/blob/main/docker-compose.yml) provisions SQL Server via `.env` (`fqdn`, `FIG_DB_NAME`, `FIG_USER_NAME`, `FIG_DB_PWD`, `SA_PWD`). See also [`.env.example`](https://github.com/mzbrau/fig/blob/main/.env.example).

Aspire can inject the connection string as `Fig` — see [.NET Aspire Integration](./guides/9-aspire-integration.md).

## Schema and backups

Fig creates and migrates its own schema on startup. Back up the SQLite file or take SQL Server backups using your usual process. Setting **values** are encrypted at rest with the API secret; protect that secret as described in [API Secret Migration](./guides/1-api-secret-migration.md).
