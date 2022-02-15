using Fig.Api;
using Fig.Api.Authorization;
using Fig.Api.Converters;
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
using Fig.Api.Validators;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.Configure<ApiSettings>(builder.Configuration.GetSection("ApiSettings"));

builder.Services.AddSingleton<IClientSecretValidator, ClientSecretValidator>();

builder.Services.AddSingleton<IFigSessionFactory, FigSessionFactory>();
builder.Services.AddScoped<IEventLogFactory, EventLogFactory>();
builder.Services.AddScoped<ITokenHandler, TokenHandler>();

builder.Services.AddSingleton<ISettingConverter, SettingConverter>();
builder.Services.AddSingleton<ISettingVerificationConverter, SettingVerificationConverter>();
builder.Services.AddScoped<ISettingDefinitionConverter, SettingDefinitionConverter>();
builder.Services.AddSingleton<ISettingVerificationResultConverter, SettingVerificationResultConverter>();
builder.Services.AddSingleton<IUserConverter, UserConverter>();
builder.Services.AddSingleton<IValueToStringConverter, ValueToStringConverter>();

builder.Services.AddSingleton<ISettingDynamicVerifier, SettingDynamicVerifier>();
builder.Services.AddSingleton<ISettingPluginVerification, SettingPluginVerification>();
builder.Services.AddSingleton<ISettingVerifier, SettingVerifier>();
builder.Services.AddSingleton<ICodeHasher, CodeHasher>();
builder.Services.AddSingleton<IValidatorApplier, ValidatorApplier>();

builder.Services.AddSingleton<ICertificateFactory, CertificateFactory>();
builder.Services.AddSingleton<ICertificateStore, CertificateStore>();

builder.Services.AddScoped<ISettingClientRepository, SettingClientClientRepository>();
builder.Services.AddSingleton<IEventLogRepository, EventLogRepository>();
builder.Services.AddSingleton<ISettingHistoryRepository, SettingHistoryRepository>();
builder.Services.AddSingleton<IVerificationHistoryRepository, VerificationHistoryRepository>();
builder.Services.AddSingleton<IUserRepository, UserRepository>();
builder.Services.AddSingleton<ICertificateMetadataRepository, CertificateMetadataRepository>();

builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<ISettingsService, SettingsService>();
builder.Services.AddScoped<IEncryptionService, EncryptionService>();

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

app.MapControllers();

app.Run();