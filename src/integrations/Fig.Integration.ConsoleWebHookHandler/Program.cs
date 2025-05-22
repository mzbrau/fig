using Fig.WebHooks.Contracts;
using Fig.Client.ExtensionMethods;
using Fig.Integration.ConsoleWebHookHandler.Configuration;
using Fig.Integration.ConsoleWebHookHandler.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// remove default logging providers
builder.Logging.ClearProviders();
// Serilog configuration        
var logger = new LoggerConfiguration()
    .WriteTo.Console()
    .Enrich.FromLogContext()
    .CreateLogger();
// Register Serilog
builder.Logging.AddSerilog(logger);

var loggerFactory = LoggerFactory.Create(b =>
{
    b.AddSerilog(logger);
});

builder.AddServiceDefaults();

var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "ConsoleWebHookHandler";
        o.LoggerFactory = loggerFactory;
        o.CommandLineArgs = args;
        o.ClientSecretOverride = "0352ee79afb2451aaf5733e047bd6c69";
    }).Build();
builder.Services.Configure<Settings>(configuration);

var app = builder.Build();

app.UseMiddleware<FigWebHookAuthMiddleware>();

app.MapPost("/NewClientRegistration",
    (ClientRegistrationDataContract dc) => Console.WriteLine(
        $"New registration for client '{dc.ClientName}' with instance '{dc.Instance}' included {dc.Settings.Count} settings. {dc.Link}"));

app.MapPost("/UpdatedClientRegistration",
    (ClientRegistrationDataContract dc) => Console.WriteLine(
        $"Updated registration for client '{dc.ClientName}' with instance '{dc.Instance}' included {dc.Settings.Count} settings."));

app.MapPost("/ClientStatusChanged",
    (ClientStatusChangedDataContract dc) => Console.WriteLine(
        $"Client {dc.ClientName} with instance '{dc.Instance}' changed to status {dc.ConnectionEvent}"));

app.MapPost("/SettingValueChanged",
    (SettingValueChangedDataContract dc) => Console.WriteLine(
        $"Client {dc.ClientName} with instance '{dc.Instance}' had the following settings updated: '{string.Join(", ", dc.UpdatedSettings)}' by user '{dc.Username}' with message '{dc.ChangeMessage}'"));

app.MapPost("/MinRunSessions",
    (MinRunSessionsDataContract dc) => Console.WriteLine(
        $"Client {dc.ClientName} with instance {dc.Instance} has {dc.RunSessions} event is:{dc.RunSessionsEvent}"));

app.Run();
