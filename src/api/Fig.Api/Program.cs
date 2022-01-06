using Fig.Api.Converters;
using Fig.Api.Datalayer;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddSingleton<ISettingConverter, SettingConverter>();
builder.Services.AddSingleton<IFigSessionFactory, FigSessionFactory>();
builder.Services.AddSingleton<ISettingDefinitionConverter, SettingDefinitionConverter>();
builder.Services.AddSingleton<ISettingClientRepository, SettingClientClientRepository>();
//builder.Services.AddSingleton<IAuditLogRepository, InMemoryAuditLogRepository>();
builder.Services.AddSingleton<ISettingsService, SettingsService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();