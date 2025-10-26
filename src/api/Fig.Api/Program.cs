using Fig.Api;
using Fig.Api.ApiStatus;
using Fig.Api.Authorization;
using Fig.Api.Converters;
using Fig.Api.Datalayer;
using Fig.Api.DataImport;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.DatabaseMigrations;
using Fig.Api.DatabaseMigrations.Migrations;
using Fig.Api.Health;
using Fig.Api.Middleware;
using Fig.Api.Observability;
using Fig.Api.Secrets;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Api.WebHost;
using Fig.Common;
using Fig.Common.Events;
using Fig.Common.NetStandard.Cryptography;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard.Validation;
using Fig.Common.Timer;
using HealthChecks.UI.Client;
using Mcrio.Configuration.Provider.Docker.Secrets;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Newtonsoft.Json;
using NHibernate;
using Serilog;
using Serilog.Core;
using System.Net;
using System.IO.Compression;
using System.Threading.RateLimiting;
using Fig.Api.ExtensionMethods;
using Fig.Api.WebHooks;
using Fig.Api.Workers;
using Fig.ServiceDefaults;
using ISession = NHibernate.ISession;

var builder = WebApplication.CreateBuilder(args);

var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .AddDockerSecrets();

IConfiguration configuration = configurationBuilder.Build();
builder.Services.Configure<ApiSettings>(configuration.GetSection("ApiSettings"));

var environment = builder.Environment.EnvironmentName ?? "Development";

var apiSettings = configuration.GetSection("ApiSettings");

var logger = CreateLogger(builder);
builder.Host.UseSerilog(logger);

builder.AddServiceDefaults(ApiActivitySource.Name);

builder.Services.Configure<ApiSettings>(apiSettings);

builder.Services.ConfigureForwardHeaders(configuration);

// Add response compression services
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true; // Enable compression for HTTPS responses
    options.Providers.Add<BrotliCompressionProvider>();
    
    // Add MIME types to be compressed
    options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
    [
        "application/json", 
        "application/javascript",
        "text/plain"
    ]);
});

builder.Services.Configure<BrotliCompressionProviderOptions>(options =>
{
    options.Level = CompressionLevel.Fastest; // Optimize for speed over compression ratio
});

builder.Services.AddSingleton<IClientSecretValidator, ClientSecretValidator>();
builder.Services.AddSingleton<IClientNameValidator, ClientNameValidator>();
builder.Services.AddSingleton<IPasswordValidator, PasswordValidator>();
builder.Services.AddSingleton<IDiagnosticsService, DiagnosticsService>();

builder.Services.AddSingleton<IFigSessionFactory, FigSessionFactory>();
builder.Services.AddSingleton<ISessionFactory>(s => s.GetService<IFigSessionFactory>()!.SessionFactory);
builder.Services.AddScoped<ISession>(s => s.GetService<ISessionFactory>()!.OpenSession());
builder.Services.AddScoped<IEventLogFactory, EventLogFactory>();
builder.Services.AddScoped<ITokenHandler, TokenHandler>();
builder.Services.AddTransient<IFileImporter, FileImporter>();
builder.Services.AddSingleton<IFileWatcherFactory, FileWatcherFactory>();
builder.Services.AddTransient<IFileMonitor, FileMonitor>();
builder.Services.AddTransient<ITimerFactory, TimerFactory>();
builder.Services.AddScoped<IApiStatusRepository, ApiStatusRepository>();
builder.Services.AddTransient<IIpAddressResolver, IpAddressResolver>();

builder.Services.AddScoped<ISettingConverter, SettingConverter>();
builder.Services.AddScoped<ISettingDefinitionConverter, SettingDefinitionConverter>();
builder.Services.AddScoped<IUserConverter, UserConverter>();
builder.Services.AddScoped<IValueToStringConverter, ValueToStringConverter>();
builder.Services.AddScoped<IEventsConverter, EventsConverter>();
builder.Services.AddScoped<IClientStatusConverter, ClientStatusConverter>();
builder.Services.AddScoped<IClientExportConverter, ClientExportConverter>();
builder.Services.AddScoped<IFigConfigurationConverter, FigConfigurationConverter>();
builder.Services.AddScoped<ILookupTableConverter, LookupTableConverter>();
builder.Services.AddScoped<IApiStatusConverter, ApiStatusConverter>();
builder.Services.AddScoped<IValidValuesHandler, ValidValuesHandler>();
builder.Services.AddScoped<IDeferredClientConverter, DeferredClientConverter>();
builder.Services.AddScoped<IWebHookClientConverter, WebHookClientConverter>();
builder.Services.AddScoped<IWebHookConverter, WebHookConverter>();
builder.Services.AddScoped<ICheckPointConverter, CheckPointConverter>();
builder.Services.AddScoped<IWebHookHealthConverter, WebHookHealthConverter>();

builder.Services.AddSingleton<IDiagnostics, Diagnostics>();
builder.Services.AddScoped<ISettingChangeRecorder, SettingChangeRecorder>();
builder.Services.AddScoped<ISettingApplier, SettingApplier>();

builder.Services.AddScoped<ISettingClientRepository, SettingClientRepository>();
builder.Services.AddScoped<IEventLogRepository, EventLogRepository>();
builder.Services.AddScoped<ISettingHistoryRepository, SettingHistoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IClientStatusRepository, ClientStatusRepository>();
builder.Services.AddScoped<ILookupTablesRepository, LookupTablesRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
builder.Services.AddScoped<ICheckPointRepository, CheckPointRepository>();
builder.Services.AddScoped<ICheckPointDataRepository, CheckPointDataRepository>();
builder.Services.AddScoped<ICheckPointTriggerRepository, CheckPointTriggerRepository>();
builder.Services.AddScoped<IDeferredClientImportRepository, DeferredClientImportRepository>();
builder.Services.AddScoped<IWebHookClientRepository, WebHookClientRepository>();
builder.Services.AddScoped<IWebHookRepository, WebHookRepository>();
builder.Services.AddScoped<ISettingChangeRepository, SettingChangeRepository>();
builder.Services.AddScoped<IDeferredChangeRepository, DeferredChangeRepository>();
builder.Services.AddScoped<ICustomActionRepository, CustomActionRepository>();
builder.Services.AddScoped<ICustomActionExecutionRepository, CustomActionExecutionRepository>();
builder.Services.AddScoped<IDatabaseMigrationRepository, DatabaseMigrationRepository>();

builder.Services.AddSingleton<IVersionHelper, VersionHelper>();
builder.Services.AddSingleton<IEventDistributor, EventDistributor>();
builder.Services.AddScoped<IWebHookDisseminationService, WebHookDisseminationService>();
builder.Services.AddSingleton<IWebHookQueue, WebHookQueue>();
builder.Services.AddScoped<IWebHookClientTestingService, WebHookClientTestingService>();
builder.Services.AddScoped<IEncryptionMigrationService, EncryptionMigrationService>();
builder.Services.AddScoped<ISchedulingService, SchedulingService>();

builder.Services.AddSingleton<IClientRegistrationLockService, ClientRegistrationLockService>();
builder.Services.AddHostedService<ClientRegistrationLockCleanupService>();
builder.Services.AddScoped<ICodeHasher, CodeHasher>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<ICryptography, Cryptography>();
builder.Services.AddScoped<IEventsService, EventsService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<IClientRunSessionRepository, ClientRunSessionRepository>();
builder.Services.AddScoped<IImportExportService, ImportExportService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<ILookupTablesService, LookupTablesService>();
builder.Services.AddScoped<IApiStatusService, ApiStatusService>();
builder.Services.AddScoped<IWebHookService, WebHookService>();
builder.Services.AddScoped<ISecretStoreHandler, SecretStoreHandler>();
builder.Services.AddScoped<ISecretStore, AzureKeyVaultSecretStore>();
builder.Services.AddScoped<ITimeMachineService, TimeMachineService>();
builder.Services.AddScoped<ICustomActionService, CustomActionService>();
builder.Services.AddScoped<IDatabaseMigrationService, DatabaseMigrationService>();
builder.Services.AddScoped<IDataCleanupService, DataCleanupService>();

builder.Services.AddHttpClient();

builder.WebHost.UseKestrel(options => options.AddServerHeader = false);

// Register database migrations
builder.Services.AddTransient<IDatabaseMigration, Migration_001_IncreaseValidationRegexLength>();
builder.Services.AddTransient<IDatabaseMigration, Migration_002_DisableTimeMachine>();

// Add background services in priority order
// DatabaseMigrationWorker must run first before other services
builder.Services.AddHostedService<DatabaseMigrationWorker>();
builder.Services.AddHostedService<ConfigFileImporter>();
builder.Services.AddHostedService<ApiStatusMonitor>();
builder.Services.AddHostedService<CheckpointTriggerWorker>();
builder.Services.AddHostedService<SchedulingWorker>();
builder.Services.AddHostedService<TimeMachineWorker>();
builder.Services.AddHostedService<WebHookProcessorWorker>();
builder.Services.AddHostedService<DataCleanupWorker>();

builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IConfigurationService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IEventsService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IImportExportService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<ISettingsService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IUserService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IStatusService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IEncryptionMigrationService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<ICustomActionService>()!);

// Add rate limiting services
var apiSettingsObject = configuration.GetSection("ApiSettings").Get<ApiSettings>();
var rateLimitingConfig = apiSettingsObject?.RateLimiting ?? new RateLimitingSettings();
if (rateLimitingConfig.GlobalPolicy.Enabled)
{
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var clientIp = GetClientIpAddress(context) ?? "anonymous";
            return RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = rateLimitingConfig.GlobalPolicy.PermitLimit,
                    Window = rateLimitingConfig.GlobalPolicy.Window,
                    QueueProcessingOrder = rateLimitingConfig.GlobalPolicy.ProcessingOrder,
                    QueueLimit = rateLimitingConfig.GlobalPolicy.QueueLimit
                });
        });

        options.OnRejected = async (context, token) =>
        {
            context.HttpContext.Response.StatusCode = 429; // Too Many Requests
            await context.HttpContext.Response.WriteAsync("Rate limit exceeded. Please try again later.", token);
        };
    });
}

builder.Services.AddCors(options =>
{
    var addresses = builder.Configuration.GetValue<string>("ApiSettings:WebClientAddresses");
    options.AddDefaultPolicy(b =>
    {
        if (addresses != null)
            b.WithOrigins(addresses)
                .AllowAnyHeader()
                .AllowAnyMethod();
        else
            b.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod();
    });
});

// Newtonsoft.Json is required because the client is .net standard and must use that serializer.
builder.Services.AddControllers().AddNewtonsoftJson(options =>
{
    options.SerializerSettings.TypeNameHandling = TypeNameHandling.Objects;
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("Database");

builder.WebHost.ConfigureHttpsListener(logger);

var app = builder.Build();

app.UseResponseCompression();

app.UseSerilogRequestLogging();

// Apply forwarded headers early so RemoteIpAddress is populated from trusted proxies
var forwardedHeaderSettingsForMiddleware = app.Services.GetRequiredService<IConfiguration>()
    .GetSection("ApiSettings").Get<ApiSettings>();
if (forwardedHeaderSettingsForMiddleware?.TrustForwardedHeaders == true)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<TransactionMiddleware>();
app.UseMiddleware<RequestCountMiddleware>();

// Add rate limiting middleware if enabled
var apiSettingsForMiddleware = app.Services.GetRequiredService<IConfiguration>()
    .GetSection("ApiSettings").Get<ApiSettings>();
var rateLimitingForMiddleware = apiSettingsForMiddleware?.RateLimiting ?? new RateLimitingSettings();
if (rateLimitingForMiddleware.GlobalPolicy.Enabled)
{
    app.UseRateLimiter();
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

app.MapHealthChecks("/_health", new HealthCheckOptions()
{
    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
});

//app.UseAuthorization();

app.UseMiddleware<CallerDetailsMiddleware>();
app.UseMiddleware<AuthMiddleware>();

app.MapControllers();

app.Run();

string? GetClientIpAddress(HttpContext context)
{
    // Only use the connection's remote IP; forwarded headers are handled centrally via middleware
    return context.Connection.RemoteIpAddress?.ToString();
}

Logger CreateLogger(WebApplicationBuilder webBuilder)
{
    return new LoggerConfiguration()
        .ReadFrom.Configuration(webBuilder.Configuration)
        .Enrich.FromLogContext()
        .CreateLogger();
}