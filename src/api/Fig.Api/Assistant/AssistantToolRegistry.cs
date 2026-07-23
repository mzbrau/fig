using System.Text;
using System.Text.RegularExpressions;
using Fig.Api.Reports;
using Fig.Api.Services;
using Fig.Common;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Assistant;

public sealed class AssistantToolRegistry : IAssistantToolRegistry
{
    private const string EmptySchema = """{"type":"object","properties":{},"additionalProperties":false}""";
    private readonly IReadOnlyDictionary<string, IAssistantTool> _tools;

    public AssistantToolRegistry(
        ISettingsService settings,
        IEventsService events,
        IStatusService status,
        ILookupTablesService lookupTables,
        ISettingGroupService settingGroups,
        IWebHookService webHooks,
        ISchedulingService scheduling,
        ITimeMachineService timeMachine,
        ICustomActionService customActions,
        IApiStatusService apiStatus,
        IReportExecutionService reportExecution,
        IVersionHelper versionHelper,
        IHttpClientFactory httpClientFactory)
    {
        var tools = new List<IAssistantTool>
        {
            Tool("list_clients",
                "List registered clients and setting definitions. Secret values are masked. When the user names a setting approximately (spaces, casing, minor typos), match it to the closest registered setting name and always state that exact name in the reply.",
                EmptySchema,
                async (_, _) => Safe(await settings.GetAllClients())),
            Tool("get_client_descriptions", "List client names, instances, and descriptions.", EmptySchema,
                async (_, _) => Safe(await settings.GetClientDescriptions())),
            Tool("get_client_settings",
                "Get non-secret settings for one client and optional instance. Match approximate setting names from the user to exact registered names and state the exact name when answering.",
                Schema(("clientName", "string", true), ("instance", "string", false)),
                async (a, _) =>
                {
                    var args = Args(a);
                    var all = await settings.GetAllClients();
                    var selected = all.Clients.Where(c =>
                        string.Equals(c.Name, RequiredString(args, "clientName"), StringComparison.OrdinalIgnoreCase) &&
                        (args["instance"] is null ||
                         string.Equals(c.Instance, args.Value<string>("instance"), StringComparison.OrdinalIgnoreCase)));
                    return Safe(selected);
                }),
            Tool("get_setting_history", "Get change history for a setting. Secret values are masked.",
                Schema(("clientName", "string", true), ("settingName", "string", true), ("instance", "string", false)),
                async (a, _) =>
                {
                    var x = Args(a);
                    return Safe(await settings.GetSettingHistory(
                        RequiredString(x, "clientName"),
                        RequiredString(x, "settingName"),
                        x.Value<string>("instance")));
                }),
            Tool("get_last_changed", "Get last-change timestamps for all accessible settings.", EmptySchema,
                async (_, _) => Safe(await settings.GetLastChangedForAllClientsAndSettings())),
            Tool("get_events", "Query audit events in a UTC time range.",
                Schema(("startTime", "string", false), ("endTime", "string", false)),
                async (a, _) =>
                {
                    var x = Args(a);
                    var (start, end) = TimeRange(x);
                    return Safe(await events.GetEventLogs(start, end));
                }),
            Tool("get_event_count", "Get the total audit event count.", EmptySchema,
                async (_, _) => Safe(await events.GetEventLogCount())),
            Tool("get_client_timeline", "Get recent setting changes for a client.",
                Schema(("clientName", "string", true), ("instance", "string", false), ("startTime", "string", false), ("endTime", "string", false)),
                async (a, _) =>
                {
                    var x = Args(a);
                    var (start, end) = TimeRange(x);
                    return Safe(await events.GetClientSettingChanges(
                        start, end, RequiredString(x, "clientName"), x.Value<string>("instance")));
                }),
            Tool("get_run_sessions", "Get client status and active run sessions.", EmptySchema,
                async (_, _) => Safe(await status.GetAll())),
            Tool("list_lookup_tables", "List lookup tables.", EmptySchema,
                async (_, _) => Safe(await lookupTables.Get())),
            Tool("get_lookup_table", "Get a lookup table by id or name.",
                Schema(("id", "string", false), ("name", "string", false)),
                async (a, _) =>
                {
                    var x = Args(a);
                    var tables = await lookupTables.Get();
                    var id = Guid.TryParse(x.Value<string>("id"), out var parsed) ? parsed : (Guid?)null;
                    var name = x.Value<string>("name");
                    var match = tables.FirstOrDefault(t =>
                        (id.HasValue && t.Id == id.Value) ||
                        (!string.IsNullOrWhiteSpace(name) &&
                         string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase)));
                    return Safe(match ?? throw new KeyNotFoundException("Lookup table not found."));
                }),
            Tool("list_setting_groups", "List setting groups.", EmptySchema,
                async (_, _) => Safe(await settingGroups.GetAllGroups())),
            Tool("get_setting_group", "Get a setting group by id or name.",
                Schema(("id", "string", false), ("name", "string", false)),
                async (a, _) =>
                {
                    var x = Args(a);
                    if (Guid.TryParse(x.Value<string>("id"), out var id))
                        return Safe(await settingGroups.GetGroup(id));
                    var groups = await settingGroups.GetAllGroups();
                    var group = groups.FirstOrDefault(g =>
                        string.Equals(g.Name, x.Value<string>("name"), StringComparison.OrdinalIgnoreCase));
                    return Safe(group ?? throw new KeyNotFoundException("Setting group not found."));
                }),
            Tool("list_webhooks", "List configured webhooks. Credentials are masked.", EmptySchema,
                async (_, _) => Safe(await webHooks.GetWebHooks())),
            Tool("list_webhook_clients", "List webhook endpoints. Credentials are masked.", EmptySchema,
                async (_, _) => Safe(await webHooks.GetClients())),
            Tool("list_deferred_changes", "List pending deferred setting changes.", EmptySchema,
                async (_, _) => Safe(await scheduling.GetAllDeferredChanges())),
            Tool("list_checkpoints", "List configuration checkpoints in a UTC range.",
                Schema(("startTime", "string", false), ("endTime", "string", false)),
                async (a, _) =>
                {
                    var (start, end) = TimeRange(Args(a));
                    return Safe(await timeMachine.GetCheckPoints(start, end));
                }),
            Tool("get_checkpoint_data", "Get checkpoint snapshot data. Secrets are masked.",
                Schema(("dataId", "string", true)),
                async (a, _) => Safe(await timeMachine.GetCheckPointData(
                    Guid.Parse(RequiredString(Args(a), "dataId"))))),
            Tool("get_custom_action_status", "Get a custom action execution status.",
                Schema(("executionId", "string", true)),
                async (a, _) => Safe(await customActions.GetExecutionStatus(
                    Guid.Parse(RequiredString(Args(a), "executionId"))))),
            Tool("get_custom_action_history", "Get custom action execution history.",
                Schema(("clientName", "string", true), ("customActionName", "string", true), ("startTime", "string", false), ("endTime", "string", false)),
                async (a, _) =>
                {
                    var x = Args(a);
                    var (start, end) = TimeRange(x);
                    return Safe(await customActions.GetExecutionHistory(
                        RequiredString(x, "clientName"),
                        RequiredString(x, "customActionName"),
                        start,
                        end));
                }),
            Tool("get_api_status",
                "Get status of currently running Fig.Api server instances (hostname, version, memory, uptime/last seen, request rates, configuration errors).",
                EmptySchema,
                async (_, _) => Safe(await apiStatus.GetAll())),
            Tool("get_api_version", "Get the Fig API version and last settings update.", EmptySchema,
                async (_, _) => Safe(new
                {
                    ApiVersion = versionHelper.GetVersion(),
                    LastSettingChange = await settings.GetLastSettingUpdate()
                })),
            Tool("list_reports",
                "List available HTML reports with id, name, category, description, and parameter metadata (name, type, required, default, lookupKind). Call this before generateReport.",
                EmptySchema,
                (_, _) => Task.FromResult(Safe(reportExecution.GetAvailableReports()))),
            Tool("search_fig_docs", "Search the official Fig documentation.",
                Schema(("query", "string", true)),
                (a, ct) => SearchDocs(httpClientFactory, RequiredString(Args(a), "query"), ct)),
            Tool("fetch_fig_doc", "Fetch text from a figsettings.com documentation URL.",
                Schema(("url", "string", true)),
                (a, ct) => FetchDoc(httpClientFactory, RequiredString(Args(a), "url"), ct)),
            Tool("propose_web_actions",
                "Propose reviewable UI actions. This never changes Fig data (except generateReport, which opens a report tab). " +
                "For updateSetting on data-grid settings, value must be an array of objects keyed by the exact column names from the setting definition. " +
                "List<string> and other single-column grids use the column name Values (or a plain string array). " +
                "Never invent column keys and never emit $type metadata. " +
                "createGroup requires groupName (optional description and data/groupedSettings); creates a local unsaved draft and opens the Groups page for Save. " +
                "createLookupTable requires lookupTableName (optional data as CSV or rows); creates a local unsaved draft and opens Lookup Tables for Save. " +
                "createInstance requires clientName and instance (new instance name) and creates a local unsaved draft. " +
                "searchSettings requires searchQuery (same token syntax as the UI search: client:, setting:, description:, instance:, value:). " +
                "It opens search and accepts the first match. " +
                "highlightSetting requires clientName and settingName (optional instance); scrolls the setting into view and highlights it briefly. " +
                "When discussing or updating a specific setting, also propose highlightSetting for that setting. " +
                "generateReport requires reportId from list_reports and optional parameters (property names from the report definition, e.g. ClientName, From, To). " +
                "Fig.Web generates the report and opens it in a new browser tab.",
                """{"type":"object","properties":{"actions":{"type":"array","items":{"type":"object","properties":{"type":{"type":"string","enum":["updateSetting","createGroup","createLookupTable","createInstance","searchSettings","highlightSetting","generateReport"]},"clientName":{"type":"string"},"instance":{"type":["string","null"]},"settingName":{"type":"string"},"value":{},"groupName":{"type":"string"},"lookupTableName":{"type":"string"},"description":{"type":"string"},"data":{},"searchQuery":{"type":"string"},"reportId":{"type":"string"},"parameters":{"type":"object","additionalProperties":true}},"required":["type"],"additionalProperties":false}}},"required":["actions"],"additionalProperties":false}""",
                (a, _) => Task.FromResult(ValidateActions(a)))
        };
        _tools = tools.ToDictionary(a => a.Name, StringComparer.Ordinal);
    }

    public IReadOnlyCollection<IAssistantTool> Tools => _tools.Values.ToArray();

    public bool TryGet(string name, out IAssistantTool? tool) => _tools.TryGetValue(name, out tool);

    private static IAssistantTool Tool(
        string name,
        string description,
        string schema,
        Func<string, CancellationToken, Task<string>> execute) =>
        new DelegateAssistantTool(name, description, schema, execute);

    private static JObject Args(string json)
    {
        try
        {
            return JObject.Parse(string.IsNullOrWhiteSpace(json) ? "{}" : json);
        }
        catch (JsonReaderException ex)
        {
            throw new ArgumentException("Tool arguments are not valid JSON.", ex);
        }
    }

    private static string RequiredString(JObject args, string name) =>
        !string.IsNullOrWhiteSpace(args.Value<string>(name))
            ? args.Value<string>(name)!
            : throw new ArgumentException($"'{name}' is required.");

    private static (DateTime Start, DateTime End) TimeRange(JObject args)
    {
        var end = DateTime.TryParse(args.Value<string>("endTime"), out var parsedEnd)
            ? parsedEnd.ToUniversalTime()
            : DateTime.UtcNow;
        var start = DateTime.TryParse(args.Value<string>("startTime"), out var parsedStart)
            ? parsedStart.ToUniversalTime()
            : end.AddDays(-30);
        return (start, end);
    }

    private static string Schema(params (string Name, string Type, bool Required)[] properties)
    {
        var propertyObject = new JObject();
        foreach (var property in properties)
            propertyObject[property.Name] = new JObject { ["type"] = property.Type };
        return new JObject
        {
            ["type"] = "object",
            ["properties"] = propertyObject,
            ["required"] = new JArray(properties.Where(a => a.Required).Select(a => a.Name)),
            ["additionalProperties"] = false
        }.ToString(Formatting.None);
    }

    private static string Safe(object? value)
    {
        var token = value is null ? JValue.CreateNull() : JToken.FromObject(value);
        MaskSecrets(token);
        StripBase64Images(token);
        var json = token.ToString(Formatting.None);
        return Truncate(json, 40_000);
    }

    private static readonly Regex MarkdownBase64ImageRegex = new(
        @"!\[[^\]]*\]\(data:image\/[a-zA-Z0-9.+-]+;base64,[^)]+\)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex RawBase64DataUriRegex = new(
        @"data:image\/[a-zA-Z0-9.+-]+;base64,[A-Za-z0-9+/=]+",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static void StripBase64Images(JToken token)
    {
        if (token is JObject obj)
        {
            foreach (var property in obj.Properties().ToList())
            {
                if (property.Value.Type == JTokenType.String)
                    property.Value = StripBase64FromString(property.Value.Value<string>() ?? string.Empty);
                else
                    StripBase64Images(property.Value);
            }
        }
        else if (token is JArray array)
        {
            for (var index = 0; index < array.Count; index++)
            {
                var item = array[index];
                if (item.Type == JTokenType.String)
                    array[index] = StripBase64FromString(item.Value<string>() ?? string.Empty);
                else
                    StripBase64Images(item);
            }
        }
    }

    internal static string StripBase64FromString(string value)
    {
        if (string.IsNullOrEmpty(value) ||
            value.IndexOf("base64,", StringComparison.OrdinalIgnoreCase) < 0)
            return value;

        var withoutMarkdown = MarkdownBase64ImageRegex.Replace(value, "[image omitted]");
        return RawBase64DataUriRegex.Replace(withoutMarkdown, "[image omitted]");
    }

    private static void MaskSecrets(JToken token)
    {
        if (token is JObject obj)
        {
            var isSecret = obj.Properties().Any(p =>
                string.Equals(p.Name, "IsSecret", StringComparison.OrdinalIgnoreCase) &&
                p.Value.Type == JTokenType.Boolean &&
                p.Value.Value<bool>());
            foreach (var property in obj.Properties().ToList())
            {
                if ((isSecret && (property.Name.Equals("Value", StringComparison.OrdinalIgnoreCase) ||
                                  property.Name.Equals("DefaultValue", StringComparison.OrdinalIgnoreCase))) ||
                    IsCredentialProperty(property.Name))
                {
                    property.Value = "[REDACTED]";
                }
                else
                {
                    MaskSecrets(property.Value);
                }
            }
        }
        else if (token is JArray array)
        {
            foreach (var item in array)
                MaskSecrets(item);
        }
    }

    private static bool IsCredentialProperty(string name) =>
        !name.Equals("IsSecret", StringComparison.OrdinalIgnoreCase) &&
        (name.Contains("Secret", StringComparison.OrdinalIgnoreCase) ||
         name.Contains("Password", StringComparison.OrdinalIgnoreCase) ||
         name.Contains("AccessToken", StringComparison.OrdinalIgnoreCase) ||
         name.Contains("ApiKey", StringComparison.OrdinalIgnoreCase));

    private static string ValidateActions(string json)
    {
        var args = Args(json);
        var actions = args["actions"] as JArray ??
                      throw new ArgumentException("'actions' must be an array.");
        var validTypes = new HashSet<string>(StringComparer.Ordinal)
        {
            "updateSetting", "createGroup", "createLookupTable",
            "createInstance", "searchSettings", "highlightSetting", "generateReport"
        };
        foreach (var action in actions.OfType<JObject>())
        {
            var type = action.Value<string>("type") ?? string.Empty;
            if (!validTypes.Contains(type))
                throw new ArgumentException("Unsupported proposed action type.");
            if (type == "generateReport" &&
                string.IsNullOrWhiteSpace(action.Value<string>("reportId")))
                throw new ArgumentException("generateReport requires reportId.");
            if (type == "createGroup" &&
                string.IsNullOrWhiteSpace(action.Value<string>("groupName")))
                throw new ArgumentException("createGroup requires groupName.");
            if (type == "createLookupTable" &&
                string.IsNullOrWhiteSpace(action.Value<string>("lookupTableName")))
                throw new ArgumentException("createLookupTable requires lookupTableName.");
        }
        return actions.ToString(Formatting.None);
    }

    private static async Task<string> SearchDocs(
        IHttpClientFactory factory,
        string query,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Post,
            "https://A1WOHSQ91H-dsn.algolia.net/1/indexes/figsettings/query");
        request.Headers.Add("X-Algolia-Application-Id", "A1WOHSQ91H");
        request.Headers.Add("X-Algolia-API-Key", "178e02980223381b845ab27d6ffa7461");
        request.Content = new StringContent(
            JsonConvert.SerializeObject(new { query, hitsPerPage = 8 }),
            Encoding.UTF8,
            "application/json");
        using var response = await factory.CreateClient("FigAssistantDocs")
            .SendAsync(request, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.EnsureSuccessStatusCode();
        return Truncate(body, 24_000);
    }

    private static async Task<string> FetchDoc(
        IHttpClientFactory factory,
        string url,
        CancellationToken cancellationToken)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) ||
            uri.Scheme != Uri.UriSchemeHttps ||
            !(uri.Host.Equals("figsettings.com", StringComparison.OrdinalIgnoreCase) ||
              uri.Host.EndsWith(".figsettings.com", StringComparison.OrdinalIgnoreCase)))
            throw new ArgumentException("Only HTTPS URLs on figsettings.com can be fetched.");

        var html = await factory.CreateClient("FigAssistantDocs").GetStringAsync(uri, cancellationToken);
        var withoutScripts = Regex.Replace(
            html,
            "<(script|style)[^>]*>.*?</\\1>",
            string.Empty,
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        var text = System.Net.WebUtility.HtmlDecode(
            Regex.Replace(withoutScripts, "<[^>]+>", " "));
        return Truncate(Regex.Replace(text, "\\s+", " ").Trim(), 24_000);
    }

    private static string Truncate(string value, int maximum) =>
        value.Length <= maximum ? value : value.Substring(0, maximum) + "...[truncated]";

    private sealed class DelegateAssistantTool : IAssistantTool
    {
        private readonly Func<string, CancellationToken, Task<string>> _execute;

        public DelegateAssistantTool(
            string name,
            string description,
            string parameterJsonSchema,
            Func<string, CancellationToken, Task<string>> execute)
        {
            Name = name;
            Description = description;
            ParameterJsonSchema = parameterJsonSchema;
            _execute = execute;
        }

        public string Name { get; }
        public string Description { get; }
        public string ParameterJsonSchema { get; }

        public Task<string> ExecuteAsync(string argumentsJson, CancellationToken cancellationToken) =>
            _execute(argumentsJson, cancellationToken);
    }
}
