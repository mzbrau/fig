using System;
using System.Threading;
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
            FigConfigurationManager<ConsoleSettings>.Initialize(options, new Fig.Client.Logging.ConsoleLogger());

            var settings = FigConfigurationManager<ConsoleSettings>.Settings;

            while (true)
            {
                Console.WriteLine($"CityName: {settings.CurrentValue.CityName}");
                Thread.Sleep(10000);
            }
            
            
            Console.ReadKey();
        }
    }
}
