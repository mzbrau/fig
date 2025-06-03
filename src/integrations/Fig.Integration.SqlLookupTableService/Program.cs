using Fig.Client.Contracts;
using Fig.Client.ExtensionMethods;
using Fig.Client.SecretProvider.Dpapi;
using Fig.Common.Timer;
using Fig.Integration.SqlLookupTableService;
using Fig.ServiceDefaults;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

static string GetBasePath() => Directory.GetParent(AppContext.BaseDirectory)?.FullName ?? string.Empty;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();

var serilogLogger = CreateLogger(builder.Configuration);

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog(serilogLogger);
});

builder.AddServiceDefaults();

IEnumerable<IClientSecretProvider> secretProviders = [new DpapiSecretProvider()];
builder.Configuration.SetBasePath(GetBasePath())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "SqlLookupTableService";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        options.ClientSecretProviders = secretProviders;
    });

builder.Host.UseSerilog(serilogLogger);

builder.Services.AddHttpClient();
builder.Services.Configure<Settings>(builder.Configuration);
builder.Services.AddHostedService<Worker>();
builder.Host.UseFig<Settings>();
builder.Services.AddSingleton<ITimerFactory, TimerFactory>();
builder.Services.AddSingleton<IFigFacade, FigFacade>();
builder.Services.AddSingleton<IHttpService, HttpService>();
builder.Services.AddSingleton<ISqlQueryManager, SqlQueryManager>();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("DatabaseHealthCheck");

var app = builder.Build();

app.MapHealthChecks("/_health", new HealthCheckOptions()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

app.Run();


Logger CreateLogger(IConfiguration configuration)
{
    return new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .WriteTo.Console(theme: AnsiConsoleTheme.Code)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .WriteTo.File(Path.Combine(Environment.SpecialFolder.ApplicationData.ToString(), "Fig", "Logs", "sql_lookup_table_service-.log"),
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30)
        .CreateLogger();
}