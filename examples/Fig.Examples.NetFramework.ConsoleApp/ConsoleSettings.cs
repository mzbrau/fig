#nullable enable
using Fig.Client;
using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Examples.NetFramework.ConsoleApp
{
    public class ConsoleSettings : SettingsBase
    {
        [Setting("The City Name")]
        public string? CityName { get; set; }

        public override string ClientDescription => "A .NET Framework Application";
        public override void Validate(ILogger logger)
        {
            logger.LogInformation("Performing validation...");
        }
    }
}