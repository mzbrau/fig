using Fig.Client.ExtensionMethods;
using Fig.Common.Timer;
using Fig.Integration.SqlLookupTableService;
using Serilog;
using Serilog.Core;
using Serilog.Sinks.SystemConsole.Themes;

var logLevel = new LoggingLevelSwitch();
var logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(logLevel)
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(Environment.SpecialFolder.ApplicationData.ToString(), "Fig", "Logs", "sql_lookup_table_service-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var loggerFactory = LoggerFactory.Create(b =>
{
    b.AddSerilog(logger);
});

var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "SQL Lookup Table Service";
        o.LoggerFactory = loggerFactory;
    }).Build();

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices(services =>
    {
        services.Configure<Settings>(configuration);
        services.AddHostedService<Worker>();
        services.AddSingleton<ITimerFactory, TimerFactory>();
        services.AddSingleton<IFigFacade, FigFacade>();
        services.AddSingleton<IHttpService, HttpService>();
        services.AddSingleton<ISqlQueryManager, SqlQueryManager>();
    })
    .Build();

await host.RunAsync();