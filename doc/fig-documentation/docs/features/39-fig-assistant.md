---
sidebar_position: 39
sidebar_label: Fig Assistant
---

# Fig Assistant

Fig Assistant is an in-app chatbot for administrators. It can answer questions about your Fig configuration and prepare **pending** changes for you to review and save.

## Enablement

Fig Assistant is **disabled by default**. An administrator enables it on the **Configuration** page:

1. Turn on **Fig Assistant**.
2. Enter an OpenAI-compatible API endpoint (for example `https://api.openai.com/v1`).
3. Enter a model name.
4. Enter an access token.
5. Click **Test** to verify connectivity.
6. Save configuration (changes save automatically when fields change).

The access token is encrypted at rest and is never returned in plaintext from the API.

When disabled, the floating assistant icon is hidden.

## Who can use it

In this version, only users with the **Administrator** role can use Fig Assistant.

## How it works

1. Click the floating Fig icon in the bottom-right corner to open the chat panel.
2. Ask a question or request a change.
3. Fig.Api runs an agent loop against your configured LLM, using read-only tools (settings metadata, events, groups, lookup tables, webhooks, scheduling, time machine, API status, reports catalogue, docs search, and more).
4. If the assistant proposes a write (update setting, create group, create lookup table, or create instance), Fig.Web applies it as a **local draft / pending change**.
5. You must press **Save** on the relevant page to persist anything.

The assistant never persists configuration changes itself.

### Status and reports

| Tool / action | Effect |
|---------------|--------|
| `get_api_status` | Returns status of currently running Fig.Api server instances (hostname, version, memory, last seen, and related fields). Connected Fig client apps are covered by `get_run_sessions`. |
| `list_reports` | Lists available HTML reports and their parameter metadata. |
| `generateReport` (via `propose_web_actions`) | Fig.Web generates the report (`POST /reports/{id}`) and opens the HTML in a new browser tab, same as the Reports page. |

Report parameters can be inferred from chat context (for example the selected client) or supplied by the user. Date ranges default to the last 7 days when not specified.

### Client-side UI actions

In addition to drafts, the assistant can propose UI-only actions:

| Action | Effect |
|--------|--------|
| `createInstance` | Creates an unsaved client instance draft (same as **Create Instance** on Settings). Requires a name. |
| `searchSettings` | Opens the settings search dialog with the given query and accepts the first match. |
| `highlightSetting` | Scrolls a setting into view and briefly highlights it (same glow as selecting a search result). |
| `generateReport` | Generates a report and opens it in a new tab (see above). |

Updating a setting also triggers a highlight for that setting so the change is easy to find.

## Privacy and secrets

- Conversation history is kept in the browser session only (cleared on logout or refresh).
- Secret setting values are not sent in UI context and are masked in tool results.
- Prompts and tool results are sent to the LLM endpoint you configure. Treat that provider as a trust boundary.

## Observability (OpenTelemetry)

When Fig.Api is running with an OTLP exporter (for example via the Aspire AppHost dashboard), each assistant chat turn produces nested activities under the inbound `POST /assistant/chat` span:

| Activity | What it shows |
|----------|----------------|
| `Assistant.Chat` | One user message turn; tags include username, page, history count. Event `assistant.request` has the inbound UI context and messages. |
| `Assistant.Llm` | One LLM iteration. Event `llm.request` includes the **full compacted messages** JSON sent to the model (system prompt, history, prior tool results). Event `llm.response` has assistant text and tool calls. Iteration `0` also records tool schemas (`llm.tools`). |
| `Assistant.Tool` | Each tool execution with arguments and (secret-masked) results. |

The automatic HttpClient span for `POST …/chat/completions` nests under `Assistant.Llm`. The LLM access token is never attached to traces.

To inspect prompts while tuning the assistant: open the Aspire dashboard → Traces → filter for `/assistant/chat` → expand `Assistant.Llm` → open the `llm.request` event and read `fig.assistant.messages`.

## Documentation search

Fig Assistant can search the official documentation at [figsettings.com](https://figsettings.com) using Algolia and optionally fetch page text from that site only.
