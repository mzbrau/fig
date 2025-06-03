using Fig.Client.Contracts;
using Fig.Client.ExtensionMethods;
using Fig.Examples.SettingsDataLoader.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

var loggerFactory = LoggerFactory.Create(b =>
{
    b.AddConsole();
});

var configuration = new ConfigurationBuilder()
    .AddFig<UserService>(o =>
    {
        o.ClientName = "UserService";
        o.ClientSecretOverride = "be633c90474448c382c47045b2e172d5xx";
        o.LoggerFactory = loggerFactory;
    })
    .AddFig<DiscountService>(o =>
    {
        o.ClientName = "DiscountService";
        o.ClientSecretOverride = "a215891d8ae14859a5e56cae9e01938xx";
        o.LoggerFactory = loggerFactory;
    })
    .AddFig<OrdersService>(o =>
    {
        o.ClientName = "OrdersService";
        o.ClientSecretOverride = "c70b28e8cd064dd48a11d8fc5b379cb0xx";
        o.LoggerFactory = loggerFactory;
    })
    .AddFig<ProductService>(o =>
    {
        o.ClientName = "ProductService";
        o.ClientSecretOverride = "9900afdd5a064a57993d883b7ef47efaxx";
        o.LoggerFactory = loggerFactory;
    }).Build();

var serviceCollection = new ServiceCollection();
serviceCollection.Configure<UserService>(configuration);
serviceCollection.Configure<DiscountService>(configuration);
serviceCollection.Configure<OrdersService>(configuration);
serviceCollection.Configure<ProductService>(configuration);
var serviceProvider = serviceCollection.BuildServiceProvider();

var userService = serviceProvider.GetRequiredService<IOptionsMonitor<UserService>>();
var discountService = serviceProvider.GetRequiredService<IOptionsMonitor<DiscountService>>();
var ordersService = serviceProvider.GetRequiredService<IOptionsMonitor<OrdersService>>();
var productService = serviceProvider.GetRequiredService<IOptionsMonitor<ProductService>>();

Console.WriteLine($"User service string setting is:{userService.CurrentValue.StringSetting}");
Console.WriteLine($"Discount service string setting is:{discountService.CurrentValue.AStringSetting}");

Console.WriteLine("Done!");
Console.ReadKey();
