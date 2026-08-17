---
sidebar_position: 13
---

# Fig Version 3.x to 4.0 Migration Guide

Fig 4.0 is the current documentation. Use this page when you upgrade from 3.x.

## Breaking changes

### Compact `GET /clients` JSON

`GET /clients` now returns a compact JSON dialect: short `t` / `v` discriminators instead of Newtonsoft `$type` for setting values. **Fig.Web** and **Fig.Mcp** already understand this format.

Fig.Client does **not** call `GET /clients`. Register and get-settings remain compatible with older and newer Fig.Api versions.

If you have a **custom** consumer of `GET /clients`, update it to the compact contract (`TypeNameHandling.None` plus the compact value converter). Do not apply that dialect to Fig.Client register/get-settings.

### Registration checksum (default on)

When you upgrade **Fig.Client**, [registration checksum](../features/37-registration-checksum.md) is enabled by default. Unchanged setting definitions skip `POST /clients` and go straight to requesting values.

To restore 3.x “always register” behaviour:

```
FIG_DISABLE_REGISTRATION_CHECKSUM=true
```

In containers, persist the Fig app-data folder (or set `FIG_APP_DATA_DIR`) so checksum files survive restarts.

### Dashboard role

A new `Dashboard` role was added. Exhaustive `switch` statements on `Role` need a new case. Map the role in Keycloak `RoleMappings` if you use OIDC. See [User Management](../features/3-user-management.md) and [Dashboards](../features/41-dashboards.md).

### Apple Silicon SQLite

The custom `external/arm64` SQLite interop workaround is removed. Fig.Api uses System.Data.SQLite 2.x native packages. Container deployments are unaffected. See [Database](../database.md).

## New capabilities worth adopting

- [Dashboards](../features/41-dashboards.md) (requires **Allow JavaScript** in Configuration)
- [Fig Assistant](../features/39-fig-assistant.md)
- [Reports](../features/38-reports.md)
- [Keycloak authentication](../security.md#keycloak-authentication-mode)
- [Custom status properties](../features/40-custom-status-properties.md)
- [Running offline without a Fig server](./11-running-offline-without-fig-server.md) / [AppSettings.json generation](../features/36-appsettings-generation.md)
- [Add Fig with AI](./0-add-with-ai.md)
- `--instance=` on the client ([Instances](../features/19-instances.md))

## Client CLI

Ensure `options.CommandLineArgs = args` so new flags work. See [Client Configuration](../client-configuration.md#command-line-arguments) for `--printappsettings`, `--figoffline`, and `--instance=`.
