// See https://aka.ms/new-console-template for more information

using Fig.Client;
using Fig.Client.Configuration;
using Fig.Examples.SettingsDataLoader.Settings;

var figOptions = new FigOptions();
figOptions.StaticUri("https://localhost:7281");
var provider = new FigConfigurationProvider(figOptions, log => Console.WriteLine(log));

UserService userService = await provider.Initialize<UserService>();
DiscountService discountService = await provider.Initialize<DiscountService>();
OrdersService ordersService = await provider.Initialize<OrdersService>();
ProductService productService = await provider.Initialize<ProductService>();

Console.WriteLine($"User service string setting is:{userService.StringSetting}");

Console.WriteLine("Done!");
Console.ReadKey();
