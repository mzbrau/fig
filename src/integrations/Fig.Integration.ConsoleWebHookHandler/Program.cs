using Fig.WebHooks.Contracts;
using Fig.Client.ExtensionMethods;
using Fig.Integration.ConsoleWebHookHandler.Middleware;
using Fig.Integration.ConsoleWebHookHandler.Settings;
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

builder.Services.AddFig<ISettings, Settings>(options =>
{
    options.ApiUri = new Uri("https://localhost:7281");
    options.ClientSecret = "0352ee79afb2451aaf5733e047bd6c69";
});

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

app.MapPost("/MemoryLeakDetected",
    (MemoryLeakDetectedDataContract dc) => Console.WriteLine(
        $"Client {dc.ClientName} with instance '{dc.Instance}' had a suspected memory leak increasing from {dc.StartingBytesAverage} " +
        $"bytes to {dc.EndingBytesAverage} bytes in {dc.SecondsAnalyzed} seconds."));

app.MapPost("/SettingValueChanged",
    (SettingValueChangedDataContract dc) => Console.WriteLine(
        $"Client {dc.ClientName} with instance '{dc.Instance}' had the following settings updated: '{string.Join(", ", dc.UpdatedSettings)}' by user '{dc.Username}'"));

app.MapPost("/MinRunSessions",
    (MinRunSessionsDataContract dc) => Console.WriteLine(
        $"Client {dc.ClientName} with instance {dc.Instance} has {dc.RunSessions} event is:{dc.RunSessionsEvent}"));

app.MapPost("/ConfigurationError",
    (ClientConfigurationErrorDataContract dc) => Console.WriteLine(
        $"Client {dc.ClientName} with instance {dc.Instance} config error status: {dc.Status}. " +
        $"{(dc.Status == ConfigurationErrorStatus.Error ? $"Errors: {string.Join(",", dc.ConfigurationErrors)}" : string.Empty)}"));

app.Run();
