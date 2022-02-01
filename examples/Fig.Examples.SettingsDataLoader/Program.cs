// See https://aka.ms/new-console-template for more information

using Fig.Api.Integration.Test.TestSettings;
using Fig.Client;
using Fig.Client.Configuration;

var figOptions = new FigOptions();
figOptions.StaticUri("https://localhost:7281");
var provider = new FigConfigurationProvider(figOptions, log => Console.WriteLine(log));

AllSettingsAndTypes allSettingsAndTypes = await provider.Initialize<AllSettingsAndTypes>();
DiscountService discountService = await provider.Initialize<DiscountService>();
OrdersService ordersService = await provider.Initialize<OrdersService>();
ProductService productService = await provider.Initialize<ProductService>();

Console.WriteLine("Done!");
Console.ReadKey();
