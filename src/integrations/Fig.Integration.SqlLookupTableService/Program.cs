using Fig.Client.ExtensionMethods;
using Fig.Common.Timer;
using Fig.Integration.SqlLookupTableService;
using Serilog;
using Serilog.Core;
using Serilog.Extensions.Logging;
using Serilog.Sinks.SystemConsole.Themes;

var logLevel = new LoggingLevelSwitch();
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.ControlledBy(logLevel)
    .Enrich.FromLogContext()
    .WriteTo.File(Path.Combine(Environment.SpecialFolder.ApplicationData.ToString(), "Fig", "Logs", "sql_lookup_table_service-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30)
    .WriteTo.Console(theme: AnsiConsoleTheme.Code)
    .CreateLogger();

var logger = new SerilogLoggerFactory(Log.Logger).CreateLogger<Program>();

logger.LogInformation("#### Starting SQL Lookup Table Service ####");

IHost host = Host.CreateDefaultBuilder(args)
    .UseSerilog()
    .ConfigureServices(services =>
    {
        services.AddHostedService<Worker>();
        services.AddSingleton<ITimerFactory, TimerFactory>();
        services.AddSingleton<IFigFacade, FigFacade>();
        services.AddSingleton<IHttpService, HttpService>();
        services.AddSingleton<ISqlQueryManager, SqlQueryManager>();
        services.AddFig<ISettings, Settings>(options =>
        {
            options.ApiUri = new Uri("http://localhost:5051");
            options.ClientSecret = "aef943d9825c4bf9a9f1b0a633e3ffc3";
        }, (settings, _) => logLevel.MinimumLevel = settings.LogLevel,
            () => Environment.Exit(0));
    })
    .Build();

await host.RunAsync();