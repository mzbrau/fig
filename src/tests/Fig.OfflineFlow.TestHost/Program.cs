using Fig.Client.ExtensionMethods;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

const string dumpBoundSettingsArg = "--dump-bound-settings";

var configuration = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .AddFig<OfflineFlowTestHostSettings>(options =>
    {
        options.ClientName = new OfflineFlowTestHostSettings().ClientName;
        options.CommandLineArgs = args;
        options.ClientSecretProviders = [new TestAppSettingsEncryptionProvider()];
    })
    .Build();

if (args.Contains(dumpBoundSettingsArg, StringComparer.Ordinal))
{
    var services = new ServiceCollection();
    services.Configure<OfflineFlowTestHostSettings>(configuration);
    var settings = services.BuildServiceProvider().GetRequiredService<IOptions<OfflineFlowTestHostSettings>>().Value;
    var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "bound-settings.json");
    File.WriteAllText(outputPath, JsonConvert.SerializeObject(settings, Formatting.Indented));
}
