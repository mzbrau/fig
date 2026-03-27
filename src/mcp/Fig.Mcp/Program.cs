using Fig.Mcp.ApiClient;
using Fig.Mcp.Configuration;
using Fig.Mcp.Tools;
using ModelContextProtocol.Server;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
builder.Services.Configure<McpSettings>(builder.Configuration.GetSection("McpSettings"));
var settings = builder.Configuration.GetSection("McpSettings").Get<McpSettings>() ?? new McpSettings();

// In stdio mode, Kestrel isn't needed but WebApplication still starts it.
// Bind to an OS-assigned port to avoid conflicts with other services.
if (settings.Transport.Equals("stdio", StringComparison.OrdinalIgnoreCase))
    builder.WebHost.UseUrls("http://127.0.0.1:0");

// ── Logging (Serilog) ────────────────────────────────────────────────────────
// In stdio mode, stdout is the MCP transport — logs MUST go to file/stderr only
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
builder.Host.UseSerilog(logger);

// ── HTTP Client for Fig API ──────────────────────────────────────────────────
builder.Services.AddTransient<FigAuthHandler>();
builder.Services.AddHttpClient<IFigApiClient, FigApiClient>(client =>
    {
        client.BaseAddress = new Uri(settings.FigApiBaseUrl.TrimEnd('/') + "/");
    })
    .AddHttpMessageHandler<FigAuthHandler>()
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback =
                HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
        }
        return handler;
    });

// ── MCP Server with gated tool registration ──────────────────────────────────
var gates = settings.ToolGates;
var mcpBuilder = builder.Services.AddMcpServer(options =>
{
    options.ServerInfo = new()
    {
        Name = "fig-mcp",
        Version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "1.0.0"
    };
});

// Transport selection
var transport = settings.Transport.ToLowerInvariant();
if (transport == "stdio")
    mcpBuilder.WithStdioServerTransport();
else
    mcpBuilder.WithHttpTransport();

// Register tool classes based on gate configuration
if (gates.ReadSettings)
{
    mcpBuilder.WithTools<ClientTools>();
    mcpBuilder.WithTools<LookupTableReadTools>();
    mcpBuilder.WithTools<WebHookReadTools>();
    mcpBuilder.WithTools<TimeMachineReadTools>();
    mcpBuilder.WithTools<SchedulingReadTools>();
    mcpBuilder.WithTools<CustomActionReadTools>();
    mcpBuilder.WithTools<StatusTools>();
}

if (gates.ReadEvents)
    mcpBuilder.WithTools<EventTools>();

if (gates.ReadSessions)
    mcpBuilder.WithTools<SessionTools>();

if (gates.ReadHistory)
    mcpBuilder.WithTools<HistoryTools>();

if (gates.WriteSettings)
    mcpBuilder.WithTools<SettingWriteTools>();

if (gates.ManageClients)
    mcpBuilder.WithTools<ClientManagementTools>();

if (gates.DeleteClients)
    mcpBuilder.WithTools<ClientDeleteTools>();

if (gates.ManageLookupTables)
    mcpBuilder.WithTools<LookupTableWriteTools>();

if (gates.ManageWebHooks)
    mcpBuilder.WithTools<WebHookWriteTools>();

if (gates.ManageTimeMachine)
    mcpBuilder.WithTools<TimeMachineWriteTools>();

if (gates.ManageScheduling)
    mcpBuilder.WithTools<SchedulingWriteTools>();

if (gates.ExecuteCustomActions)
    mcpBuilder.WithTools<CustomActionWriteTools>();

if (gates.ManageUsers)
    mcpBuilder.WithTools<UserTools>();

if (gates.ImportExportData)
    mcpBuilder.WithTools<DataTools>();

if (gates.ManageConfiguration)
    mcpBuilder.WithTools<ConfigurationTools>();

// ── Build & Run ──────────────────────────────────────────────────────────────
var app = builder.Build();

if (transport != "stdio")
    app.MapMcp();

app.Run();
