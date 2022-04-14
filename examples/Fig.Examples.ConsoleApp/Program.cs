// See https://aka.ms/new-console-template for more information

using Fig.Client;
using Fig.Client.Configuration;
using Fig.Examples.ConsoleApp;

var figOptions = new FigOptions();
figOptions.StaticUri("https://localhost:7281");
var provider = new FigConfigurationProvider(figOptions, log => Console.WriteLine(log));

IConsoleSettings settings = await provider.Initialize<ConsoleSettings>();

Console.WriteLine("Settings were:");
Console.WriteLine($"Favourite Animal: {settings.FavouriteAnimal}");
Console.WriteLine($"Favourite Number: {settings.FavouriteNumber}");
Console.WriteLine($"True or False: {settings.TrueOrFalse}");

Console.ReadKey();