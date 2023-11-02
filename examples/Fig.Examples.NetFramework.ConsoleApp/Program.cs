using System;
using Fig.Client.Configuration;
using Fig.Client.NetFramework;

namespace Fig.Examples.NetFramework.ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var options = new FigOptions
            {
                ClientName = "DotnetConsoleApp"
            };
            FigConfigurationManager<FrameworkConsoleSettings>.Initialize(options, new Fig.Client.Logging.ConsoleLogger());

            var settings = FigConfigurationManager<FrameworkConsoleSettings>.Settings;

            Console.WriteLine($"CityName: {settings.CurrentValue.CityName}");

            Console.ReadKey();
        }
    }
}
