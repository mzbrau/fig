---
sidebar_position: 1
---

# Why Fig?

Every service needs configuration: API URLs, timeouts, credentials, logging levels. Those values change by environment, and they need to stay consistent across many microservices.

In modern .NET you typically bind `appsettings.json`, environment variables, and other providers into `IOptions<T>` / `IOptionsMonitor<T>`. That pattern is typed, testable, and can reload. It still leaves gaps when you operate a fleet of services:

- Changing the same setting across many applications
- Sharing values (for example a connection string) without copy-paste
- Knowing who changed a value, when, and why
- Validating types and ranges before a bad value reaches production
- Seeing which instances are running and whether they picked up the last change

Fig is itself a configuration provider, so it fits the same `IOptions<T>` pattern and can sit alongside other sources. Applications declare settings with attributes. On startup they register with the Fig API. Operators then edit values in Fig Web, with editors, descriptions, history, and access control.

Fig is designed so configuration can be managed across many microservices in a secure, auditable way without giving up the ASP.NET configuration model.

:::note .NET Framework

Older .NET Framework apps often used `app.config` and `ConfigurationManager.AppSettings`. Fig.Client still supports Framework hosts via `FigConfigurationManager<T>` (see the [NetFramework console example](https://github.com/mzbrau/fig/tree/main/examples/Fig.Examples.NetFramework.ConsoleApp)), but new integrations should use the ASP.NET Core `AddFig` / `UseFig` path.

:::
