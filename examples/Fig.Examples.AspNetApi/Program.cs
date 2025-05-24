using Fig.Client.ExtensionMethods;
using Fig.Examples.AspNetApi;
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

builder.Configuration.SetBasePath(GetBasePath())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
    .AddFig<Settings>(options =>
    {
        options.ClientName = "AspNetApi";
        options.LoggerFactory = loggerFactory;
        options.CommandLineArgs = args;
    });

builder.Host.UseSerilog(serilogLogger);

builder.Services.AddControllers();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.Configure<Settings>(builder.Configuration);

builder.Host.UseFig<Settings>();

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

Logger CreateLogger(IConfiguration configuration)
{
    return new LoggerConfiguration()
        .ReadFrom.Configuration(configuration)
        .WriteTo.Console(theme: AnsiConsoleTheme.Code)
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
        .Enrich.FromLogContext()
        .CreateLogger();
}