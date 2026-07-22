---
sidebar_position: 38
sidebar_label: Reports
---

# Reports

Fig includes a server-side reporting framework that generates printable HTML reports. Administrators select a report, supply parameters, and open the result in a new browser tab where they can print or **Save as PDF** using the browser print dialog.

Reports are **administrator only** and are **compiled into Fig.Api**—users cannot add reports at runtime. The Blazor web client only knows report metadata (id, name, category, description, parameters)—all data access and HTML rendering happen on the API. Report results respect the caller’s `ClientFilter`; clients (and client-tagged events) outside that regex are excluded.

## Built-in reports

### Clients & users (original)

| Report | Category | Parameters | Contents |
|--------|----------|------------|----------|
| User Activity | Users | User, From, To | Logins, setting changes, restarts, and other authenticated actions |
| Setting History | Settings | Client, Instance (optional), Setting | Category, markdown description, current value (nested table for data grids), and historical changes |
| Client Status | Clients | Client, Instance (optional) | All settings grouped by category (secret values shown as `******`) |
| Client History | Clients | Client, Instance (optional), From, To | Registrations, configuration changes, and events |
| Client Uptime | Clients | Client, Instance (optional), From, To | Availability (online when any run session is active), outages, peak concurrent sessions, run session log |

The **Client Uptime** report treats the client as available whenever **at least one** run session is online. Redundant instances of the same client therefore do not create downtime when one of them stops; individual session starts and ends appear in the Run Session Log, while Outages list only periods with zero active sessions.

### Security & compliance

| Report | Category | Parameters | Contents |
|--------|----------|------------|----------|
| Security Audit | Security | From, To | Failed logins, invalid client secrets, secret/password/user lifecycle, and configuration changes |
| Configuration Inventory | Compliance | Client (optional), Secrets Only | Fleet inventory of setting flags (secret, externally managed, classification, env-specific, etc.) without secret values |
| Access & Privilege | Security | From, To | Users with role, client filter, classifications, login/fail counts |
| Secret Hygiene | Security | Client (optional) | Aged secret settings, previous client-secret window, API secret-rotation state (values always masked) |
| Externally Managed Overrides | Compliance | From, To, Client (optional) | Externally managed inventory and operator override events |

### Operations

| Report | Category | Parameters | Contents |
|--------|----------|------------|----------|
| Fleet Health & Availability | Operations | From, To, Client (optional), Min Sessions | Live health/session debt plus period uptime rollup across clients |
| Registration Drift | Operations | From, To | Definition-changed registrations, new instances, clients with no recent registration |
| Restart & Live-Reload Debt | Operations | From, To | Sessions needing restart or with live reload off; historical restart activity |
| Client Version | Operations | From, To | Connected clients with app/Fig versions; Fig version pie; multi-version apps |
| Instance / Environment Matrix | Operations | Client | Setting-value matrix across all instances of a client; divergence highlights |
| Deferred / Scheduled Work | Operations | From, To | Pending/overdue deferred changes, schedule events, deferred imports |
| Time Machine Activity | Operations | From, To | Checkpoint create/apply/note activity |
| Import / Export Activity | Operations | From, To | Export/import/deferred-import event history |

### Analytics

| Report | Category | Parameters | Contents |
|--------|----------|------------|----------|
| Change Analytics | Analytics | From, To, Client (optional) | Top changers, daily volume, externally managed share, unchanged settings |
| Blast Radius | Analytics | Client, Setting, Include Group Members | Clients/settings affected via groups; live sessions; recent related changes |
| Anomaly / Quiet Period | Analytics | From, To | Spike detection vs prior baseline; clients that went quiet |
| Stale Config | Analytics | From, To, Stale Days | Settings unchanged for N days; silent/orphaned clients |
| Incident Correlation Pack | Analytics | Client, From, To | Chronological ops narrative for a client window with outages |

### Integrations & platform

| Report | Category | Parameters | Contents |
|--------|----------|------------|----------|
| Webhook Delivery | Integrations | From, To | Configured hooks vs sends; zero-send hooks; session-flap noise context |
| Custom Action Outcomes | Integrations | From, To, Client (optional) | Execution counts, success rate, failures, slowest actions |
| Setting Groups Coverage | Compliance | Group (optional) | Group membership and value divergence across members |
| Lookup Usage | Compliance | — | Lookups used by settings vs unused; client-defined flag |
| Fig Platform Self-Report | Platform | — | API nodes, feature gates, inventory counts, event-log volume |

Optional **Client** parameters support an **All clients** choice in the Reports UI.

## Using reports

1. Sign in as an administrator.
2. Open **Reports** from the main navigation.
3. Select a report from the left list.
4. Fill in the required parameters (dropdowns are provided for users, clients, settings, and groups where applicable).
5. Click **Generate Report**.
6. The HTML report opens in a new tab. Use **Print / Save as PDF** (or the browser print dialog) to export.

:::tip
Allow pop-ups for the Fig web origin so the generated report tab can open.
:::

## Security

- Only users with the **Administrator** role can list or generate reports.
- Secret settings are listed on the Client Status report with values shown as `******` (never the real value). Secret columns inside data grids are masked the same way.
- Setting History shows `<SECRET>` placeholders for secret settings instead of real values.
- Inventory, hygiene, groups, and matrix reports never render real secret values.
- Event history values that were already stored as placeholders remain masked.

## Output format

Version 1 supports **HTML only**. Additional formats (PDF, Excel, CSV) are reserved for future versions; the API already accepts a `format` field defaulting to `Html`.

When you print or save as PDF, each page includes a footer with **Generated by Fig** and the generation timestamp on the left, and the current and total page numbers on the right (supported in Chrome, Edge, and Safari). In the browser print dialog, turn off the browser’s default headers and footers so they do not duplicate Fig’s footer.

## For developers

See [Adding a Report](../guides/12-adding-reports.md) for how to implement a new report with one class, one Razor view, and one DI registration.
