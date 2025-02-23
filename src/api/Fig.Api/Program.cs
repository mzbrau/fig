using Fig.Api;
using Fig.Api.ApiStatus;
using Fig.Api.Appliers;
using Fig.Api.Authorization;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Health;
using Fig.Api.Middleware;
using Fig.Api.Observability;
using Fig.Api.Secrets;
using Fig.Api.Services;
using Fig.Api.SettingVerification;
using Fig.Api.SettingVerification.Converters;
using Fig.Api.SettingVerification.ExtensionMethods;
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
using Newtonsoft.Json;
using NHibernate;
using Serilog;
using Serilog.Core;
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
builder.Services.AddScoped<ISettingVerificationConverter, SettingVerificationConverter>();
builder.Services.AddScoped<ISettingDefinitionConverter, SettingDefinitionConverter>();
builder.Services.AddScoped<ISettingVerificationResultConverter, SettingVerificationResultConverter>();
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

builder.Services.AddScoped<ISettingVerification, SettingVerification>();
builder.Services.AddScoped<IVerificationApplier, VerificationApplier>();
builder.Services.AddSingleton<IDiagnostics, Diagnostics>();
builder.Services.AddScoped<ISettingChangeRecorder, SettingChangeRecorder>();
builder.Services.AddScoped<ISettingApplier, SettingApplier>();

builder.Services.AddScoped<ISettingClientRepository, SettingClientRepository>();
builder.Services.AddScoped<IEventLogRepository, EventLogRepository>();
builder.Services.AddScoped<ISettingHistoryRepository, SettingHistoryRepository>();
builder.Services.AddScoped<IVerificationHistoryRepository, VerificationHistoryRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IClientStatusRepository, ClientStatusRepository>();
builder.Services.AddScoped<ILookupTablesRepository, LookupTablesRepository>();
builder.Services.AddScoped<IConfigurationRepository, ConfigurationRepository>();
builder.Services.AddScoped<ICheckPointRepository, CheckPointRepository>();
builder.Services.AddScoped<ICheckPointDataRepository, CheckPointDataRepository>();
builder.Services.AddScoped<IDeferredClientImportRepository, DeferredClientImportRepository>();
builder.Services.AddScoped<IWebHookClientRepository, WebHookClientRepository>();
builder.Services.AddScoped<IWebHookRepository, WebHookRepository>();
builder.Services.AddScoped<ISettingChangeRepository, SettingChangeRepository>();
builder.Services.AddSingleton<IVersionHelper, VersionHelper>();
builder.Services.AddSingleton<IEventDistributor, EventDistributor>();
builder.Services.AddScoped<IWebHookDisseminationService, WebHookDisseminationService>();
builder.Services.AddScoped<IWebHookClientTestingService, WebHookClientTestingService>();
builder.Services.AddScoped<IEncryptionMigrationService, EncryptionMigrationService>();

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

builder.Services.AddHttpClient();
builder.Services.AddSettingVerifiers();

builder.Services.AddHostedService<ConfigFileImporter>();
builder.Services.AddHostedService<ApiStatusMonitor>();
builder.Services.AddHostedService<CheckpointWorker>();

builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IConfigurationService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IEventsService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IImportExportService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<ISettingsService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IUserService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IStatusService>()!);
builder.Services.AddScoped<IAuthenticatedService>(a => a.GetService<IEncryptionMigrationService>()!);

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

app.UseSerilogRequestLogging();

app.UseMiddleware<TransactionMiddleware>();
app.UseMiddleware<ErrorHandlerMiddleware>();
app.UseMiddleware<RequestCountMiddleware>();

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

Logger CreateLogger(WebApplicationBuilder webBuilder)
{
    return new LoggerConfiguration()
        .ReadFrom.Configuration(webBuilder.Configuration)
        .Enrich.FromLogContext()
        .CreateLogger();
}