using Fig.Client.ExtensionMethods;
using Fig.Client.SecretProvider.Docker;
using Fig.Client.SecretProvider.Dpapi;
using Fig.Examples.Yarp;
using Fig.ServiceDefaults;
using Serilog;
static string GetBasePath() => Directory.GetParent(AppContext.BaseDirectory)?.FullName ?? string.Empty;

var builder = WebApplication.CreateBuilder(args);

var serilogLogger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(builder =>
{
    builder.AddSerilog(serilogLogger);
});

builder.AddServiceDefaults();

builder.Configuration.SetBasePath(GetBasePath())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "Yarp Example";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
        options.ClientSecretProviders = [new DockerSecretProvider(), new DpapiSecretProvider()];
        options.ClientSecretOverride = "f984efe5b49b40ffaf53428cec9530b8";
    });

// Use Serilog for logging
builder.Host.UseSerilog(serilogLogger);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

// Register the configuration logger
builder.Services.AddHostedService<ReverseProxyConfigLogger>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapReverseProxy();

app.Run();
