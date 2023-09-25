// See https://aka.ms/new-console-template for more information

using Fig.Client;
using Fig.Client.Configuration;
using Fig.Client.Factories;
using Fig.Client.Logging;
using Fig.Examples.SettingsDataLoader.Settings;

const string ApiAddress = "https://localhost:7281";

var userServiceOptions = new FigOptions
{
    ApiUri = new Uri(ApiAddress),
    ClientSecret = "be633c90474448c382c47045b2e172d5"
};
var loggerFactory = new SimpleLoggerFactory();
var httpClientFactory = new SimpleHttpClientFactory(userServiceOptions.ApiUri);
var userServiceProvider = FigConfigurationProvider.Create(loggerFactory, userServiceOptions, httpClientFactory);
UserService userService = await userServiceProvider.Initialize<UserService>();

var discountServiceOptions = new FigOptions
{
    ApiUri = new Uri(ApiAddress),
    ClientSecret = "a215891d8ae14859a5e56cae9e019385"
};
var discountServiceProvider = FigConfigurationProvider.Create(loggerFactory, discountServiceOptions, httpClientFactory);
DiscountService discountService = await discountServiceProvider.Initialize<DiscountService>();

var ordersServiceOptions = new FigOptions
{
    ApiUri = new Uri(ApiAddress),
    ClientSecret = "c70b28e8cd064dd48a11d8fc5b379cb0"
};
var ordersServiceProvider = FigConfigurationProvider.Create(loggerFactory, ordersServiceOptions, httpClientFactory);
OrdersService ordersService = await ordersServiceProvider.Initialize<OrdersService>();

var productServiceOptions = new FigOptions
{
    ApiUri = new Uri(ApiAddress),
    ClientSecret = "9900afdd5a064a57993d883b7ef47efa"
};
var productServiceProvider = FigConfigurationProvider.Create(loggerFactory, productServiceOptions, httpClientFactory);
ProductService productService = await productServiceProvider.Initialize<ProductService>();

Console.WriteLine($"User service string setting is:{userService.StringSetting}");
Console.WriteLine($"Discount service string setting is:{discountService.AStringSetting}");

Console.WriteLine("Done!");
Console.ReadKey();
