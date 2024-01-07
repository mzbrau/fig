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
    }).Build();

var serviceCollection = new ServiceCollection();
serviceCollection.Configure<Settings>(configuration);

var serviceProvider = serviceCollection.BuildServiceProvider();

var settings = serviceProvider.GetRequiredService<IOptionsMonitor<Settings>>();

//Console.WriteLine(settings.CurrentValue.ServiceUsername);

Console.ReadKey();