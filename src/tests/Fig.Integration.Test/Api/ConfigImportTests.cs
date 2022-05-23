using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fig.Integration.Test.Api.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class ConfigImportTests : IntegrationTestBase
{
    [Test]
    public async Task ShallImportConfigurationFromFile()
    {
        var path = GetConfigImportPath();
        await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();

        var data = await ExportData(true);
        var import = JsonConvert.SerializeObject(data);

        await DeleteAllClients();

        var clients1 = (await GetAllClients()).ToList();
        Assert.That(clients1.Count, Is.Zero);

        var exportFile = Path.Combine(path, "dataImport.json");
        await File.WriteAllTextAsync(exportFile, import);

        // Wait enough time for the file to be imported.
        await Task.Delay(200);

        var clients2 = (await GetAllClients()).ToList();

        Assert.That(clients2.Count, Is.EqualTo(2));
        Assert.That(File.Exists(exportFile), Is.False, "Import file should have been deleted");
    }
}