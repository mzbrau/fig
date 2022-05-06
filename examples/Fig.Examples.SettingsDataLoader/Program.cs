// See https://aka.ms/new-console-template for more information

using Fig.Client;
using Fig.Client.Configuration;
using Fig.Client.Logging;
using Fig.Examples.SettingsDataLoader.Settings;

const string ApiAddress = "https://localhost:7281";

var userServiceOptions = new FigOptions().WithApiAddress(ApiAddress).WithSecret("be633c90474448c382c47045b2e172d5");
var userServiceProvider = new FigConfigurationProvider(new ConsoleLogger(), userServiceOptions);
UserService userService = await userServiceProvider.Initialize<UserService>();

var discountServiceOptions = new FigOptions().WithApiAddress(ApiAddress).WithSecret("a215891d8ae14859a5e56cae9e019385");
var discountServiceProvider = new FigConfigurationProvider(new ConsoleLogger(), discountServiceOptions);
DiscountService discountService = await discountServiceProvider.Initialize<DiscountService>();

var ordersServiceOptions = new FigOptions().WithApiAddress(ApiAddress).WithSecret("c70b28e8cd064dd48a11d8fc5b379cb0");
var ordersServiceProvider = new FigConfigurationProvider(new ConsoleLogger(), ordersServiceOptions);
OrdersService ordersService = await ordersServiceProvider.Initialize<OrdersService>();

var productServiceOptions = new FigOptions().WithApiAddress(ApiAddress).WithSecret("9900afdd5a064a57993d883b7ef47efa");
var productServiceProvider = new FigConfigurationProvider(new ConsoleLogger(), productServiceOptions);
ProductService productService = await productServiceProvider.Initialize<ProductService>();

Console.WriteLine($"User service string setting is:{userService.StringSetting}");
Console.WriteLine($"Discount service string setting is:{discountService.AStringSetting}");

Console.WriteLine("Done!");
Console.ReadKey();
