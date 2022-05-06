// See https://aka.ms/new-console-template for more information

using Fig.Client;
using Fig.Client.Configuration;
using Fig.Client.Logging;
using Fig.Examples.ConsoleApp;

var figOptions = new FigOptions();
figOptions.WithApiAddress("https://localhost:7281");
var provider = new FigConfigurationProvider(new ConsoleLogger(), figOptions);

IConsoleSettings settings = await provider.Initialize<ConsoleSettings>();

Console.WriteLine("Settings were:");
Console.WriteLine($"Favourite Animal: {settings.FavouriteAnimal}");
Console.WriteLine($"Favourite Number: {settings.FavouriteNumber}");
Console.WriteLine($"True or False: {settings.TrueOrFalse}");

settings.SettingsChanged += (sender, eventArgs) =>
{
    Console.WriteLine($"{DateTime.Now}: Settings have changed!");
    Console.WriteLine("Settings were:");
    Console.WriteLine($"Favourite Animal: {settings.FavouriteAnimal}");
    Console.WriteLine($"Favourite Number: {settings.FavouriteNumber}");
    Console.WriteLine($"True or False: {settings.TrueOrFalse}");
};

Console.ReadKey();