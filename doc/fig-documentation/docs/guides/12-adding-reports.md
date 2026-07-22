---
sidebar_position: 12
sidebar_label: Adding a Report
---

# Adding a Report

Reports are **compiled into Fig.Api**. Each report is a C# class plus a Razor view, registered at application startup. End users **cannot** add, upload, or configure new reports through the UI or API—adding a report requires a code change, rebuild, and redeploy.

Fig’s reporting framework is designed so that adding a report requires:

1. One strongly typed parameter class and report class
2. One Razor body component
3. One DI registration (`AddReport<T>()` inside `AddFigReports()`)

No controller changes, no Web UI changes (unless you introduce a new parameter lookup kind), and no database tables.

## 1. Define parameters

```csharp
public class MyReportParameters
{
    [ReportParameter("Client", LookupKind = ReportParameterLookupKind.Clients)]
    public string ClientName { get; set; } = string.Empty;

    [ReportParameter("Instance")]
    public string? Instance { get; set; }

    [ReportParameter("From")]
    public DateTime From { get; set; }

    [ReportParameter("To")]
    public DateTime To { get; set; }
}
```

Metadata for `GET /reports` is generated automatically from these properties (`ReportParameterAttribute`, nullability, and type).

Lookup kinds:

- `Users` — username dropdown
- `Clients` — client (+ instance) dropdown; optional client parameters get an **All clients** choice in the UI
- `ClientSettings` — setting names for the selected client
- `Groups` — setting group name dropdown; optional group parameters get an **All groups** choice in the UI
- `None` — free-form editor based on type (`String`, `DateTime`, `Bool`, `Int`, `Guid`)

`bool` / `int` / `long` parameters are optional unless marked `[Required]`; omitted values keep property initializers (e.g. `int StaleDays { get; set; } = 90`). `DateTime` and non-nullable strings remain required by default.

## 2. Implement the report

Subclass `ReportBase<TParameters, TModel>` under `Fig.Api/Reports/Implementations/`. `ReportBase` implements `IAuthenticatedService`; AuthMiddleware populates `AuthenticatedUser` on every request.

```csharp
public class MyReport : ReportBase<MyReportParameters, MyReportModel>
{
    public override string Id => "my-report";
    public override string Name => "My Report";
    public override string Category => "Clients";
    public override string Description => "Short description shown in the catalogue.";
    public override Type BodyComponentType => typeof(MyReportView);

    public override async Task<object> ExecuteAsync(
        MyReportParameters parameters,
        CancellationToken cancellationToken = default)
    {
        // Load data via existing repositories/services — do not run ad-hoc SQL here.
        var model = new MyReportModel { /* ... */ };
        return model;
    }
}
```

Guidelines:

- Prefer existing repositories (`IEventLogRepository`, `ISettingHistoryRepository`, etc.).
- Keep heavy calculations in C# (see `UptimeCalculator`).
- Never hydrate or render secret setting values; mask them (e.g. `******` on Client Status) or use `SecretConstants.SecretPlaceholder`.
- Prefer `GetClient` over `GetClientReadOnly` when you need to iterate settings (read-only evicts the entity from the NHibernate session).
- Respect the caller’s client filter: pass `AuthenticatedUser` into fleet loads (`GetAllClients`, `GetEventsByTypes`, …) and call `ThrowIfNoAccess(clientName)` before single-client loads. Never pass `null` as the requesting user from a report.

## 3. Create the Razor body

Add a view under `Fig.Api/Reports/Rendering/Views/`:

```razor
@namespace Fig.Api.Reports.Rendering.Views
@using Fig.Api.Reports.Rendering.Components

@if (Model is not null)
{
    <section class="report-section">
        <h2>Summary</h2>
        <SummaryCards Items="Model.Summary" />
    </section>
}

@code {
    [Parameter]
    public MyReportModel? Model { get; set; }
}
```

Reusable components:

- `SummaryCards`
- `ReportTable`
- `Timeline`
- `RichText`
- `ReportChart` (Chart.js CDN for print-friendly charts)

The shared `ReportDocument` layout adds the Fig logo, title, generated-by metadata, parameter summary, print toolbar, and print CSS.

## 4. Register with DI

Register the report inside `AddFigReports()` in `Fig.Api/Reports/ReportServiceCollectionExtensions.cs`:

```csharp
services.AddReport<MyReport>();
```

`Program.cs` only calls `builder.Services.AddFigReports();` once. `AddReport<T>()` also registers the report as `IAuthenticatedService` so the request user is populated.

## 5. API surface (no changes needed)

| Method | Route | Role |
|--------|-------|------|
| `GET` | `/reports` | Administrator — catalogue metadata |
| `POST` | `/reports/{reportId}` | Administrator — body `ReportExecutionRequestDataContract`, returns `text/html` |

Report data is scoped to the caller’s `ClientFilter`. Users only see clients (and client-tagged events) that match their filter.

## 6. Tests

- Unit-test pure helpers (parameter metadata, binders, calculators).
- Add an integration test in `Fig.Integration.Test/Api/ReportsTests.cs` that executes your report and asserts the HTML contains the expected title and does not leak secrets.

## Extensibility notes

- New output formats: implement `IReportRenderer` for another `ReportFormat` value.
- New lookup kinds: extend `ReportParameterLookupKind` and the Reports page editors in Fig.Web.
- Scheduling, email delivery, and AI summaries are intentionally out of scope for V1.
