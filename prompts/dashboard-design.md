# Fig Dashboards — Implementation Specification

## 1. Overview

Implement a new **Dashboards** feature spanning **Fig.Api** (persistence and authorization) and the **Fig Web Client** (Blazor WebAssembly editor, runtime, and viewer).

Dashboards allow Fig users to create configurable, read-only views of Fig data using a visual dashboard builder.

A dashboard consists of:

1. A visual layout containing dashboard components on a flat grid.
2. Optional reusable JavaScript transformations (including a DAG of named transforms).
3. Optional component-specific JavaScript transformations.
4. Optional built-in presets that generate transforms for simple users.
5. Component configuration.
6. A responsive 12-column grid-based layout.
7. Refresh configuration (dashboard-specific policy).
8. Metadata (`AdminOnly`, description, etc.).

**Runtime and transforms execute in Blazor WebAssembly.** Dashboard **definitions** are persisted and authorized via Fig.Api.

**Jint** is already used by Fig.Web for display scripts (`JintEngine` / `JintEngineFactory`) and must be reused for dashboard transformations, subject to the same safety limits.

The system must not allow arbitrary HTML or Blazor/Razor code to be supplied by users. JavaScript is strictly a **data transformation/configuration mechanism**. Rendering is always performed by trusted Fig Blazor components.

---

# 2. Goals

The dashboard system should allow users to create dashboards such as:

* Number of currently connected run sessions.
* Number of run sessions by client name.
* Number of run sessions by application version.
* Charts showing version distributions.
* Tables showing client/run-session health.
* Lists of run sessions with selected metadata.
* Settings displayed across multiple clients (non-secret, classification-filtered).
* Aggregated information across clients / run sessions.
* Latest/earliest values from custom status properties.
* Counts, sums, averages, minimums and maximums.
* Conditional status indicators.
* Multiple visualisations of the same transformed data.
* Reusable data transformations shared by multiple components.

The system should support both simple and advanced users:

### Simple user

Drag a component onto the canvas and configure it via a **built-in preset** (e.g. “number of run sessions”) or dropdowns. Presets fill in a transform under the hood.

### Power user

Write a JavaScript transformation to convert Fig data into the format required by a component.

### Advanced dashboard

Create reusable named transformations (including transforms that depend on other named transforms) and have multiple components consume their results.

---

# 3. Non-goals

The following are explicitly out of scope:

* Public dashboards (unauthenticated access).
* Revocable share links / owner–ACL sharing models.
* Arbitrary user-provided HTML.
* Arbitrary user-provided CSS.
* Arbitrary Razor/Blazor components.
* User-provided executable .NET code.
* AI-assisted dashboard construction.
* Interactive dashboards that modify Fig data.
* Dashboard controls that write back to Fig.
* Server-side dashboard transformation execution.
* Real-time push updates (Fig has no SignalR/SSE today; HTTP polling only).
* Nested layout component trees (Container / Stack / nested Grid).
* Collapsible sections.
* Line charts (no general historical time-series store for status).
* Light/dark theme switching (Fig.Web is dark-only today).
* Mobile-specific dashboard design tooling in the first version.
* Exposing secret setting values (secrets are not sent to the web application).

Dashboards are fundamentally **read-only presentations of Fig data**.

---

# 4. Existing Fig data

The Fig Web Client already has data models representing:

## 4.1 Client settings

Primary web model: `SettingClientConfigurationModel` (and related setting models).

Relevant concepts include:

* Client `Name`, `Instance`, `Description`
* Settings collection (values, metadata, classification)
* Run-session summary fields on the client (`CurrentRunSessions`, `CurrentHealth`, etc.)

API load path: `GET /clients` (Fig.Web load dialect via `FigWebLoadJsonSettings`).

These existing models should be adapted into **dashboard-specific API wrappers** so the Jint-facing API is stable. Do not expose internal NHibernate entities or raw data contracts to scripts.

**Secrets:** Secret setting values must not appear in the dashboard JS API (they are not delivered to the web app anyway).

**Classifications:** Honour the viewer’s `AllowedClassifications` when projecting settings into `fig.clients`.

**Client filter:** Honour the viewer’s `ClientFilter` regex when projecting clients and run sessions.

## 4.2 Run sessions / client status

Primary web model: `ClientRunSessionModel`.

Includes (among others):

* `Name`, `Instance`
* `RunSessionId`
* `LastSeen`, `StartTimeUtc`, `LastRegistration`
* `LastSettingValueUpdateUtc`, `LastSettingLoadUtc`
* `IpAddress`, `Hostname`
* `FigVersion`, `ApplicationVersion`
* `RunningUser`, `MemoryUsageBytes`
* `PollIntervalMs`, `LiveReload`
* `Health` (`RunSessionHealthModel` / `FigHealthStatus`)
* `CustomProperties` (`CustomStatusPropertyModel`: name, display name, value type, value, highlight, showInUi, order, etc.)

API load path: `GET /statuses` (and related property endpoints as needed).

There is **no** client `type` field and **no** nested `machine` object in the real models. Use real names (`ApplicationVersion`, `Hostname`, etc.) in the dashboard API.

---

# 5. Dashboard data API

Jint must not receive arbitrary internal Fig domain objects.

Expose a deliberate dashboard API:

```javascript
fig.clients
fig.runSessions
```

Exact C# wrapper types should follow existing Fig conventions.

## 5.1 Run sessions (`fig.runSessions`)

Expose connected run sessions through a stable model derived from `ClientRunSessionModel` / status contracts.

Conceptually (illustrative — final property set must match adapted real fields):

```javascript
{
    name: "...",
    instance: "...",
    runSessionId: "...",
    applicationVersion: "...",
    figVersion: "...",
    hostname: "...",
    ipAddress: "...",
    lastSeen: "2026-08-11T14:32:00Z",   // ISO-8601 UTC
    startTimeUtc: "...",
    runningUser: "...",
    memoryUsageBytes: 12345678,
    health: { /* status + components */ },
    customProperties: {
        /* name → value / metadata as designed for JS */
    }
}
```

## 5.2 Client settings (`fig.clients`)

Expose settings-client information through the dashboard API so transforms can answer:

* What is the value of setting X for every client?
* Which clients have a particular value?
* How many clients have a particular value?
* What is the distribution of values?
* What are the values for a particular client name/instance?

Always apply viewer `ClientFilter`, `AllowedClassifications`, and secret exclusion.

---

# 6. JavaScript execution

Use the existing Fig.Web Jint infrastructure (`IJsEngine` / `JintEngine` / `JintEngineFactory`).

Display scripts today expose setting wrappers only. Dashboards require a **new** Jint environment that injects `fig.clients` / `fig.runSessions` and helpers — not the display-script `SettingWrapper` surface — while reusing the same engine factory and limits.

Jint scripts are responsible for:

* Filtering, selecting, mapping, grouping, aggregating, sorting
* Calculating derived values
* Producing the exact data structure expected by a component

Jint must **not** be responsible for rendering.

Do not expose DOM APIs, Blazor APIs, or arbitrary .NET APIs.

---

# 7. JavaScript helper API

Provide a small, predictable set of helpers suitable for dashboard data manipulation.

At minimum, support equivalents of:

```javascript
filter()
map()
groupBy()
sort()
take()
distinct()
count()
sum()
average()
min()
max()
first()
last()
```

The implementation may expose these as methods on collections, helper functions, or another idiomatic mechanism. Some may already be available in the base JavaScript environment.

Example:

```javascript
fig.runSessions
    .filter(s => s.name === "OrdersService")
    .map(s => ({
        name: s.name,
        version: s.applicationVersion
    }));
```

And:

```javascript
fig.runSessions
    .groupBy(s => s.applicationVersion)
    .map(group => ({
        label: group.key,
        value: group.items.length
    }));
```

---

# 8. Dashboard transformation model

Support two types of JavaScript transformations, plus presets.

## 8.1 Named reusable transformations

A dashboard can define reusable transformations identified by stable IDs/names.

A reusable transformation produces a result which can be consumed by multiple components **and by other named transformations** (see §9).

Example:

```javascript
return fig.runSessions
    .groupBy(s => s.name)
    .map(group => ({
        name: group.key,
        count: group.items.length,
        latestSeen: group.items
            .map(s => s.lastSeen)
            .sort()
            .at(-1)
    }));
```

## 8.2 Component-specific (inline) transformations

A component can have its own transformation for simple dashboards where a reusable transform is unnecessary.

## 8.3 Built-in presets

v1 includes a small set of presets that configure a component and supply a transform, for example:

* Count of run sessions
* Count of run sessions by `applicationVersion`
* Count of run sessions by client `name`
* Table of run sessions with selected columns

Presets may expose the generated script as editable for power users.

---

# 9. Transformation dependency model

```text
Dashboard
│
├── Transform: sessionsByName
├── Transform: sessionVersions
│
├── KPI
│    └── uses sessionsByName
│
├── Chart
│    └── uses sessionVersions
│
└── Table
     └── uses sessionsByName
```

A component may consume:

* Raw dashboard API data (via an inline transform), or
* A reusable named transformation

Named transformations may depend on other named transformations (**DAG**).

Components must **not** depend on other components’ outputs.

The implementation must detect and report circular transformation dependencies.

---

# 10. Component input contracts

Every visual component must define an explicit input contract.

A component should not need to understand Fig data directly.

```text
Fig data
    ↓
Jint transformation
    ↓
Component input model
    ↓
Blazor component
```

Examples:

Bar / pie chart:

```javascript
[
    { label: "1.0.0", value: 42 },
    { label: "1.1.0", value: 17 }
]
```

Table: array of row objects; columns configured separately.

KPI:

```javascript
{
    value: 127,
    label: "Connected run sessions",
    trend: 12
}
```

Contracts should be strongly typed on the C# side.

---

# 11. Initial component library

## 11.1 KPI / Number

Prominent value, label, optional secondary value, optional trend, status/variant.

## 11.2 Text

Plain text and heading variants; optional expression-derived content.

## 11.3 Badge / Status

Semantic variants only (no arbitrary CSS), e.g. `normal`, `info`, `success`, `warning`, `danger`, `muted`.

## 11.4 Bar chart

Vertical/horizontal bars, labels, values, optional legend/title/value display.

Fig.Web has **no chart library today**; introduce an appropriate chart dependency for Web (and reuse or parallel assets for HTML export).

## 11.5 Pie / donut chart

Same label/value input shape as bar chart aggregations.

## 11.6 Table

Dynamically defined columns (header, property, formatting, alignment, sortability, semantic variant where appropriate).

## 11.7 List

Collection of items (run sessions, alerts-style rows, machines/hostnames, etc.).

## 11.8 Key/value

Small set of labelled values.

**Out of scope for v1:** line charts, collapsible sections, nested layout components.

---

# 12. Layout

Use a **flat 12-column grid** only.

Each component has:

```text
id
x
y
width
height
```

Do not use arbitrary absolute pixel positioning as the persisted representation.

No nested Container / Stack / Grid component trees. No collapsible regions.

Fig.Web has no grid/drag-drop library today; add a suitable dependency or implement the minimum required behaviour.

---

# 13. Responsive behaviour

* Desktop: 12-column layout.
* Smaller screens: automatically reduce columns / stack components.
* No separate mobile layout editor in v1.
* Do not make responsive behaviour part of the first persisted schema unless necessary.

---

# 14. Dashboard visual editor

Three major areas:

```text
┌────────────┬──────────────────────────┬──────────────┐
│ Components │          Canvas           │ Properties   │
│            │                          │              │
│ KPI        │                          │              │
│ Chart      │       dashboard          │              │
│ Table      │                          │              │
│ Text       │                          │              │
│ List       │                          │              │
└────────────┴──────────────────────────┴──────────────┘
```

**Save model:** Explicit save with dirty flag. Optimistic concurrency via `lastModified` (or equivalent token): on conflict, warn and require reload or force-overwrite. No autosave.

---

# 15. Component palette

Organize into categories:

```text
Data
  KPI
  Table
  List
  Key/value

Charts
  Bar
  Donut

Content
  Text
  Badge
```

(No Layout category in v1 — layout is the canvas grid itself.)

---

# 16. Drag and drop

Users must be able to:

* Drag components onto the canvas
* Move components
* Resize components
* Reorder components where applicable
* Delete components
* Duplicate components

---

# 17. Properties editor

Selecting a component displays its properties, generated from the component schema where practical.

Include data source / preset / transformation editing without hardcoding unrelated editor logic per component type outside the registry.

---

# 18. Data transformation editor

Each data-bound component must support configuring:

* Preset (simple path)
* Reusable named transformation
* Component (inline) transformation

The editor must provide:

* Syntax editing (prefer reusing existing Monaco integration patterns where practical)
* Execute/test
* Validation
* Error display
* Result preview

---

# 19. Data explorer

While editing, allow inspection of the dashboard API roughly as:

```text
Run sessions
  name
  instance
  applicationVersion
  hostname
  lastSeen
  health
  customProperties
    ...

Clients
  name
  instance
  settings
    ...
```

Where metadata is available, show type, description, example values, and display metadata (including custom status property metadata).

---

# 20. Transformation preview

When editing a transformation, preview its output (limited rows for large results).

The preview must never cause the dashboard to retrieve data more frequently than the configured source refresh interval.

---

# 21. Error handling

Jint errors must be user-friendly (line/column when available).

One broken component must not prevent the rest of the dashboard from rendering; failed components show an error state with “View error”.

---

# 22. Jint execution limits

Follow existing Fig.Web `JintEngine` limits (currently: timeout default **5s**, `MaxStatements(1000)`, `LimitMemory(10_000_000)`, `LimitRecursion(100)`), plus existing infinite-loop detection patterns where applicable.

A dashboard transformation must not freeze the WebAssembly application indefinitely.

Do not expose unrestricted .NET access to dashboard scripts.

---

# 23. Refresh architecture

These intervals are **dashboard runtime policy**, not a change to Settings / Connected Clients page polling (those pages today poll status every ~10–15s; settings definitions are not periodically refreshed).

## Settings (`fig.clients`)

* Maximum auto-refresh frequency: **once every 10 minutes**
* Refresh on dashboard open
* Support manual refresh subject to the same minimum interval

## Run-session status (`fig.runSessions`)

* Maximum auto-refresh frequency: **once every 1 minute**
* Default: 1 minute
* Support manual refresh subject to the same minimum interval

Rationale: wallboards may run for long periods and must not hammer the API.

The dashboard should not refresh both data sets together unnecessarily. Maintain separate caches per source. Do not create one independent timer per widget.

When a source refreshes, recalculate only transforms/components that depend on that source.

---

# 24. Dashboard data dependency tracking

The runtime should know which components/transforms depend on which data sources (`settings` vs `status`) and only recalculate affected subgraphs on refresh.

---

# 25. Dashboard persistence (Fig.Api)

Persist dashboards as versioned documents via Fig.Api (NHibernate entity + JSON document blob), following patterns similar to setting groups / lookup tables:

* Business entity + map + repository + service + controller + data contracts
* Web facade/converter/models for editor and viewer

Conceptually:

```json
{
  "schemaVersion": 1,
  "name": "Production Overview",
  "description": "Overview of connected production clients",
  "adminOnly": false,
  "refresh": {
    "settingsSeconds": 600,
    "statusSeconds": 60
  },
  "transforms": [],
  "components": [],
  "layout": []
}
```

The exact persisted structure should follow existing Fig persistence and Newtonsoft serialization conventions (`TypeNameHandling` rules in AGENTS.md — prefer intentional DTOs; do not casually enable polymorphic type handling on user documents).

---

# 26. Component IDs

Every component must have a stable unique ID (not array index). Required for editing, duplication, layout, versioning, and future diffing.

---

# 27. Transformation IDs

Reusable transformations have stable IDs/names. The dashboard model references transformations by ID rather than duplicating scripts into every component.

---

# 28. Dashboard schema versioning

The dashboard document must contain `schemaVersion`. Design for future migrations; do not assume schema v1 is final.

---

# 29. Dashboard management page

Add a **Dashboards** page to Fig.Web following existing navigation conventions.

Provide:

* List of dashboards (filtered by role / `AdminOnly`)
* Create / open / edit / duplicate / delete / rename (edit actions: **Administrator** only)
* Optional description

### `Dashboard` role UX

* Web chrome: **Dashboards only** (no Settings, Connected Clients, Import/Export, etc.)
* Landing: dashboard list; if exactly one visible dashboard exists, open it directly
* Support **fullscreen / wallboard mode** (hide chrome; button and/or query flag)

---

# 30. Dashboard view mode

View mode is read-only.

Users can:

* View dashboard components
* Wait for / trigger allowed data refreshes
* Enter fullscreen / wallboard mode

Users cannot:

* Edit data
* Modify Fig settings
* Modify run sessions
* Trigger mutations through dashboard JavaScript

Clear transition from view → edit for Administrators.

---

# 31. Permissions

Extend `Fig.Contracts.Authentication.Role` with a new value:

```text
Administrator
User
LookupService
ReadOnly
Dashboard   // new
```

| Role | View non-admin dashboards | View `AdminOnly` dashboards | Create/edit/delete |
|------|---------------------------|-----------------------------|--------------------|
| Administrator | Yes | Yes | Yes |
| User | Yes | No | No |
| ReadOnly | Yes | No | No |
| Dashboard | Yes | No | No |
| LookupService | No | No | No |

Notes:

* Do **not** invent share links, per-dashboard ACLs, or owners in v1.
* All authenticated users who can view dashboards see the same non-`AdminOnly` set (still subject to **data** scoping via `ClientFilter` / classifications / secrets).
* `Dashboard` exists so a wallboard can be left signed in without access to other Fig pages or settings UI.
* API: allow `Dashboard` on read endpoints required for dashboard runtime (`GET /statuses`, `GET /clients`, and new dashboard GET endpoints) while keeping mutating APIs denied.
* Web: `[Authorize]` / route guards must hide all non-dashboard navigation for `Dashboard` role.

Mark dashboards **`adminOnly`** to restrict visibility to Administrators.

---

# 32. Sharing dashboards

Users must log in. There is no public access and no share-link feature.

Visibility rules are entirely role + `adminOnly` as in §31.

---

# 33. HTML export

Allow export of the current dashboard as a **standalone HTML snapshot** (late phase).

Must contain current definition + current data values + CSS + chart rendering needed for a static view.

Must not require Fig after export and must not execute Jint against live Fig data.

**Parity:** best-effort visual resemblance to the Blazor dashboard — not pixel-perfect. Chart rendering in the export may use a small dedicated JS helper.

Capture the **current (dark) theme** only.

---

# 34. Export architecture

Do not invent a separate dashboard format for HTML export. Reuse the same definition + snapshot data with a dedicated HTML renderer path.

---

# 35. Import/export of dashboard definitions

Support JSON **definition** import/export on the existing **Import/Export** page (Administrator-only today — acceptable because only Administrators author dashboards).

A definition export contains schema version, metadata, transforms, components, layout, configuration.

It must not contain credentials, live data snapshots, or secret values.

**Import always creates a new dashboard** (never silent overwrite).

HTML snapshot export remains on the dashboard view/edit UI, not on Import/Export.

---

# 36. Styling

Do not allow arbitrary CSS. Controlled visual properties and semantic variants only.

Use existing Fig / Bootstrap / Radzen patterns so dashboards look native to Fig.

---

# 37. Theme support

Fig.Web is **dark-only** today. Dashboards must work with that theme.

Use existing CSS variables / Radzen tokens where available so a future light theme would not require a full rewrite, but **do not** implement light/dark switching as part of this feature.

---

# 38. Performance

Because transforms execute in WebAssembly, performance matters.

Avoid:

* Repeatedly converting the same data into Jint objects
* Executing identical transformations multiple times
* Independent timers per component
* Rendering when unrelated data changes
* Loading unnecessarily large datasets into every component

Prefer: data source → cached dashboard API model → reusable transform → multiple components.

Remain responsive with hundreds of run sessions.

---

# 39. Large data sets

Tables/lists must not blindly render thousands of DOM elements — virtualize where appropriate.

Charts should aggregate or limit data where necessary.

Protect against accidentally enormous transform outputs.

---

# 40. Date/time handling

Prefer **ISO-8601 UTC strings** in the JS API for timestamps (`lastSeen`, `startTimeUtc`, etc.), matching Fig’s UTC storage conventions.

Formatting for display is handled by component/configuration layers (and existing humanization patterns where useful), not by requiring every transform to format dates.

Document the UTC convention in the data explorer / docs.

---

# 41. Custom property metadata

Reuse `CustomStatusPropertyModel` / contract metadata (`ValueType`, display name, highlight, order, etc.) in the dashboard API and data explorer.

---

# 42. Type validation

Validate transformation results against component input contracts in the editor where practical; otherwise runtime validation with clear errors.

---

# 43. Component definition architecture

Central registry (e.g. `DashboardComponentRegistry`) discovering:

* Component type, display name, category
* Input contract
* Configuration schema
* Blazor component type
* Editor metadata
* Available presets

The visual editor consumes this registry rather than large hardcoded `if/else` trees.

---

# 44. Suggested internal architecture

Adapt to existing Fig project layout (Api / Web / Contracts / Datalayer). Conceptual Web-side shape:

```text
Dashboards/
├── Models/
├── Runtime/
│   ├── DashboardRuntime
│   ├── DashboardDataProvider
│   ├── DashboardTransformEngine
│   ├── DashboardDependencyResolver
│   └── DashboardRefreshManager
├── Components/
│   ├── DashboardComponentRegistry
│   ├── Kpi/
│   ├── Table/
│   ├── Chart/
│   ├── Text/
│   ├── List/
│   └── ...
├── Editor/
└── Export/
    └── DashboardHtmlExporter
```

Api-side: entity, map, repository, service, controller, data contracts for CRUD.

---

# 45. Testing requirements

Follow Fig’s testing strategy (AGENTS.md): API tests as primary safety net for persistence/auth; unit tests for transform engine, validation, dependency graph; Playwright only for critical journeys.

## Unit tests

* Serialization / deserialization / schema validation
* Component input validation
* Transformation execution, aggregation, grouping
* Dependency resolution and cycle detection
* Refresh scheduling and data-source dependency tracking
* Schema migration
* HTML export (snapshot assembly)
* Preset → transform generation
* Role / `adminOnly` filtering helpers

## Jint tests

Representative transforms, e.g.:

### Count run sessions

```javascript
return fig.runSessions.length;
```

### Group by application version

```javascript
return fig.runSessions
    .groupBy(s => s.applicationVersion)
    .map(g => ({
        label: g.key,
        value: g.items.length
    }));
```

### Filtering, missing custom properties, invalid script isolation

## API tests

* CRUD dashboards
* Authorization matrix (including `Dashboard` and `LookupService`)
* `adminOnly` visibility
* Import creates new dashboard

## Playwright (few)

* Admin creates and views a simple preset dashboard
* `Dashboard` role lands on dashboards-only UX / fullscreen path

---

# 46. Example target dashboard

Capable of producing something equivalent to:

```text
┌───────────────────────────────────────────────────────┐
│ Production Clients                                    │
├───────────────┬───────────────┬───────────────────────┤
│ Connected     │ Client Names  │ Top version           │
│               │               │                       │
│     127       │      6        │       1.4.3           │
├───────────────┴───────────────┴───────────────────────┤
│ Client versions (bar)                                 │
│  1.4.3 ███████████████████████████  82               │
│  1.4.2 ███████████                  31               │
│  1.4.1 ████                         14               │
├───────────────────────────────┬───────────────────────┤
│ Sessions by client name       │ Health                │
│ Server       82               │ ✓ 104 Healthy        │
│ Desktop      31               │ ⚠ 18 Degraded        │
│ Mobile       14               │ ✕ 5 Unhealthy         │
├───────────────────────────────┴───────────────────────┤
│ Type/Name  Count    Latest Seen         Version       │
│ Server     82       2 minutes ago       1.4.3         │
│ Desktop    31       8 minutes ago       1.4.2         │
│ Mobile     14       1 minute ago        1.4.3         │
└───────────────────────────────────────────────────────┘
```

Grouped values are derived dynamically from `fig.runSessions` (e.g. max/latest `lastSeen` within a group).

---

# 47. Implementation approach

Implement incrementally. Prefer one commit per phase.

## Phase 1 — Foundation

* Dashboard contracts, Api entity/persistence, CRUD endpoints, auth hooks
* Dashboard API models (`fig.clients`, `fig.runSessions`)
* Jint dashboard environment + basic transform engine
* Schema versioning + basic runtime
* Role enum: add `Dashboard`

## Phase 2 — Component framework

* Component registry + contracts
* KPI, Text, Badge, Table, Bar, Pie/Donut, List, Key/value
* Flat grid host (view mode)

## Phase 3 — Editor

* Dashboards page + management
* Palette, drag/drop, grid canvas, resize
* Properties panel, presets, transform editor, data explorer, preview
* Explicit save + concurrency token

## Phase 4 — Runtime polish

* Settings / status refresh policy, dependency tracking, caching
* Error isolation, performance passes
* `Dashboard` role chrome, landing, fullscreen

## Phase 5 — Export

* Definition JSON on Import/Export (create-on-import)
* HTML snapshot export (best-effort parity)

## Phase 6 — Testing / polish

* Unit, Jint, API, and limited Playwright coverage
* Responsive behaviour
* Accessibility pass
* Performance testing with large run-session counts

---

# 48. Design principles

### Data and presentation are separate

Jint transforms data; Blazor components render data.

### Components have explicit contracts

### Reuse transformations (including transform DAGs)

### Simple things stay simple (presets)

### Advanced users have full transformation power

### Definitions are portable and server-persisted

### No arbitrary UI execution

### Performance is a first-class concern

### Version everything

### Follow existing Fig conventions

Reuse models, API patterns, authentication/roles, design system, Jint infrastructure, Newtonsoft serialization rules, persistence mechanisms, and testing conventions.

Do not introduce a new framework where an existing Fig mechanism already provides the required functionality.

---

# 49. Codebase alignment notes

Facts this design was reconciled against (code is authoritative if they drift later):

* Fig.Web is Blazor WASM; theme is dark-only (Radzen dark base).
* Jint is used for display scripts in Web (`JintEngine` limits as above); not used in Api for this purpose.
* Roles prior to this feature: `Administrator`, `User`, `LookupService`, `ReadOnly`. `User` can edit settings — not “read-only.”
* Status polling on existing pages is ~10–15s; dashboard refresh policy is intentionally slower.
* No SignalR; no existing chart or dashboard grid library in Web.
* Import/Export page is Administrator-only settings I/O today; dashboard definition I/O extends that page.
* Persist user documents via NHibernate + contracts (see lookup tables / setting groups).

---

# 50. Definition of done

The feature is considered complete when a Fig user can:

1. Open **Dashboards**.
2. Create a new dashboard (Administrator).
3. Drag components onto a grid canvas.
4. Configure properties and/or pick a preset.
5. Connect a component to Fig data via transform.
6. Write a Jint transformation and preview results.
7. Create reusable named transformations and use one in multiple components (including transform→transform DAG).
8. Aggregate `fig.runSessions` and display non-secret, filtered `fig.clients` data.
9. Respect refresh caps (settings ≥10 minutes, status ≥1 minute).
10. Save and reload with the same layout and configuration (explicit save, concurrency warning).
11. View in read-only mode; Administrators can return to edit.
12. Use `AdminOnly` dashboards visible only to Administrators.
13. Sign in as `Dashboard` role and only access dashboards (wallboard/fullscreen friendly).
14. Have viewer `ClientFilter` and `AllowedClassifications` applied to dashboard data.
15. Export definition JSON via Import/Export (import creates new).
16. Export a standalone HTML snapshot (best-effort visuals, dark theme).
17. Survive future schema changes via `schemaVersion` / migration.
18. Recover gracefully from invalid JavaScript or invalid component data.
19. Remain usable on different screen sizes within the flat grid’s responsive behaviour.

The resulting system should feel like a **native Fig feature**, not an embedded third-party dashboard framework.
