using Fig.Client.ExtensionMethods;
using Fig.Client.SecretProvider.Docker;
using Fig.Client.SecretProvider.Dpapi;
using Fig.Integration.MicrosoftSentinel.Api;
using Fig.Integration.MicrosoftSentinel.Configuration;
using Fig.Integration.MicrosoftSentinel.CustomActions;
using Fig.Integration.MicrosoftSentinel.Handlers;
using Fig.Integration.MicrosoftSentinel.Middleware;
using Fig.Integration.MicrosoftSentinel.Services;
using HealthChecks.UI.Client;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Remove default logging providers
builder.Logging.ClearProviders();

// Create a temporary logger for Fig configuration
// TODO: Bring in the log level worker to avoid creating 2 loggers.
var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();
Log.Logger = logger;

var tempLoggerFactory = LoggerFactory.Create(b =>
{
    b.AddSerilog(logger);
});

var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "Fig Sentinel Connector";
        o.LoggerFactory = tempLoggerFactory;
        o.CommandLineArgs = args;
        o.ClientSecretProviders = [new DockerSecretProvider(), new DpapiSecretProvider()];
    }).Build();

builder.Services.Configure<Settings>(configuration);

// Register the properly configured logger with the DI container before building
builder.Logging.AddSerilog(logger);

builder.Host.UseFig<Settings>();

// Register services
builder.Services.AddHttpClient<ISentinelService, SentinelService>();
builder.Services.AddScoped<IWebHookHandler, WebHookHandler>();
builder.Services.AddScoped<TestSentinelConnectionAction>();

// Add health checks
builder.Services.AddHealthChecks()
    .AddCheck<SentinelHealthCheck>("sentinel");

var app = builder.Build();

// Add webhook authentication middleware, but skip it for health checks
app.UseWhen(
    ctx => !ctx.Request.Path.StartsWithSegments("/_health"),
    b => b.UseMiddleware<FigWebHookAuthMiddleware>());

app.MapHealthChecks("/_health", new HealthCheckOptions()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

// Map webhook endpoints using the extension method
app.MapWebHookEndpoints();

logger.Information("Fig Microsoft Sentinel Integration starting...");

app.Run();

logger.Information("Fig Microsoft Sentinel Integration stopped");