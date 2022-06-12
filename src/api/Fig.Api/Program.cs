using Fig.Api;
using Fig.Api.ApiStatus;
using Fig.Api.Authorization;
using Fig.Api.Converters;
using Fig.Api.DataImport;
using Fig.Api.Datalayer;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Encryption;
using Fig.Api.Middleware;
using Fig.Api.Services;
using Fig.Api.SettingVerification;
using Fig.Api.SettingVerification.Converters;
using Fig.Api.SettingVerification.Dynamic;
using Fig.Api.SettingVerification.ExtensionMethods;
using Fig.Api.SettingVerification.Plugin;
using Fig.Api.Utils;
using Fig.Api.Validators;
using Fig.Common.IpAddress;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

var logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .CreateLogger();

builder.Host.UseSerilog(logger);

// Add services to the container.

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddSingleton<IClientSecretValidator, ClientSecretValidator>();
builder.Services.AddSingleton<IPasswordValidator, PasswordValidator>();

builder.Services.AddSingleton<IFigSessionFactory, FigSessionFactory>();
builder.Services.AddScoped<IEventLogFactory, EventLogFactory>();
builder.Services.AddScoped<ITokenHandler, TokenHandler>();
builder.Services.AddTransient<IFileImporter, FileImporter>();
builder.Services.AddSingleton<IFileWatcherFactory, FileWatcherFactory>();
builder.Services.AddSingleton<IBackgroundWorker, BackgroundWorker>();
builder.Services.AddTransient<IFileMonitor, FileMonitor>();
builder.Services.AddTransient<IConfigFileImporter, ConfigFileImporter>();
builder.Services.AddTransient<IApiStatusMonitor, ApiStatusMonitor>();
builder.Services.AddTransient<ITimerFactory, TimerFactory>();
builder.Services.AddTransient<IApiStatusRepository, ApiStatusRepository>();
builder.Services.AddTransient<IIpAddressResolver, IpAddressResolver>();

builder.Services.AddSingleton<ISettingConverter, SettingConverter>();
builder.Services.AddSingleton<ISettingVerificationConverter, SettingVerificationConverter>();
builder.Services.AddScoped<ISettingDefinitionConverter, SettingDefinitionConverter>();
builder.Services.AddSingleton<ISettingVerificationResultConverter, SettingVerificationResultConverter>();
builder.Services.AddSingleton<IUserConverter, UserConverter>();
builder.Services.AddSingleton<IValueToStringConverter, ValueToStringConverter>();
builder.Services.AddSingleton<IEventsConverter, EventsConverter>();
builder.Services.AddSingleton<IClientStatusConverter, ClientStatusConverter>();
builder.Services.AddScoped<IClientExportConverter, ClientExportConverter>();
builder.Services.AddScoped<IFigConfigurationConverter, FigConfigurationConverter>();
builder.Services.AddScoped<ICommonEnumerationConverter, CommonEnumerationConverter>();
builder.Services.AddScoped<IValidValuesHandler, ValidValuesHandler>();

builder.Services.AddSingleton<ISettingDynamicVerifier, SettingDynamicVerifier>();
builder.Services.AddSingleton<ISettingPluginVerification, SettingPluginVerification>();
builder.Services.AddSingleton<ISettingVerifier, SettingVerifier>();
builder.Services.AddSingleton<ICodeHasher, CodeHasher>();
builder.Services.AddSingleton<IValidatorApplier, ValidatorApplier>();

builder.Services.AddSingleton<ICertificateFactory, CertificateFactory>();
builder.Services.AddSingleton<ICertificateStore, CertificateStore>();

builder.Services.AddScoped<ISettingClientRepository, SettingClientRepository>();
builder.Services.AddScoped<IEventLogRepository, EventLogRepository>();
builder.Services.AddScoped<ISettingHistoryRepository, SettingHistoryRepository>();
builder.Services.AddScoped<IVerificationHistoryRepository, VerificationHistoryRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddScoped<ICertificateMetadataRepository, CertificateMetadataRepository>();
builder.Services.AddScoped<IClientStatusRepository, ClientStatusRepository>();
builder.Services.AddScoped<ICommonEnumerationsRepository, CommonEnumerationsRepository>();
builder.Services.AddSingleton<IConfigurationRepository, ConfigurationRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();
builder.Services.AddScoped<IEventsService, EventsService>();
builder.Services.AddScoped<IStatusService, StatusService>();
builder.Services.AddScoped<IImportExportService, ImportExportService>();
builder.Services.AddScoped<IConfigurationService, ConfigurationService>();
builder.Services.AddScoped<ICommonEnumerationsService, CommonEnumerationsService>();

builder.Services.AddSettingVerificationPlugins();
builder.Services.AddCertificateManager();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
        builder.WithOrigins("https://localhost:7148")
            .AllowAnyHeader()
            .AllowAnyMethod());
});

// Newtonsoft.Json is required because the client is .net standard and must use that serializer.
builder.Services.AddControllers().AddNewtonsoftJson();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSerilogRequestLogging();
app.UseMiddleware<ErrorHandlerMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseCors();

//app.UseAuthorization();

app.UseMiddleware<CallerDetailsMiddleware>();
app.UseMiddleware<AuthMiddleware>();

var backgroundWorker = app.Services.GetService<IBackgroundWorker>();
if (backgroundWorker is not null)
    await backgroundWorker.Initialize();

app.MapControllers();

app.Run();