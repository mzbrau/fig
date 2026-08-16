using Fig.Client.ExtensionMethods;
using Fig.Examples.DisplayScript;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var loggerFactory = LoggerFactory.Create(b =>
{
    b.AddConsole();
});

var configuration = new ConfigurationBuilder()
    .AddFig<Settings>(o =>
    {
        o.ClientName = "DisplayScriptExample";
        o.ClientSecretOverride = "be633c90474448c382c47045b2e172d5xx";
        o.LoggerFactory = loggerFactory;
        o.CommandLineArgs = args;
    }).Build();

var serviceCollection = new ServiceCollection();
serviceCollection.Configure<Settings>(configuration);

var serviceProvider = serviceCollection.BuildServiceProvider();

_ = serviceProvider.GetRequiredService<IOptionsMonitor<Settings>>();

Console.WriteLine("DisplayScriptExample registered with Fig. Waiting until cancelled...");

var exit = new ManualResetEventSlim(false);
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    exit.Set();
};

AppDomain.CurrentDomain.ProcessExit += (_, _) => exit.Set();

exit.Wait();
