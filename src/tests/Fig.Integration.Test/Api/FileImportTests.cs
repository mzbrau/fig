using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Fig.Common.NetStandard.Json;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class FileImportTests : IntegrationTestBase
{
    [Test]
    public async Task ShallImportConfigurationFromFile()
    {
        var path = GetConfigImportPath();
        await RegisterSettings<ThreeSettings>();
        await RegisterSettings<ClientA>();

        var data = await ExportData(true);
        var import = JsonConvert.SerializeObject(data, JsonSettings.FigDefault);

        await DeleteAllClients();

        var clients1 = (await GetAllClients()).ToList();
        Assert.That(clients1.Count, Is.Zero);

        var exportFile = Path.Combine(path, "dataImport.json");
        await File.WriteAllTextAsync(exportFile, import);

        await WaitForCondition(async () => (await GetAllClients()).Count() == 2, TimeSpan.FromSeconds(10));

        var clients2 = (await GetAllClients()).ToList();

        Assert.That(clients2.Count, Is.EqualTo(2));
        Assert.That(File.Exists(exportFile), Is.False, "Import file should have been deleted");
    }
}