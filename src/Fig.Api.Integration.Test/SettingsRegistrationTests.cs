using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Fig.Api.Integration.Test;

public class SettingsRegistrationTests : IntegrationTestBase
{
    [SetUp]
    public async Task Setup()
    {
        await DeleteAllClients();
    }
    
    [TearDown]
    public async Task TearDown()
    {
        await DeleteAllClients();
    }
    
    [Test]
    public async Task ShallRegisterSingleSetting()
    {
        var settings = await RegisterThreeSettings();

        var clients = await GetAllClients();

        var clientsList = clients.ToList();
        
        Assert.That(clientsList.Count(), Is.EqualTo(1));
        Assert.That(clientsList.First().Name, Is.EqualTo(settings.ClientName));
        Assert.That(clientsList.First().Settings.Count, Is.EqualTo(3));
    }
    
    [Test]
    public async Task ShallRegisterMultipleSettings()
    {
        await RegisterThreeSettings();
        await RegisterNoSettings();
        await RegisterOneStringSetting();
        await RegisterAllSettingsAndTypes();

        var clients = await GetAllClients();

        var clientsList = clients.ToList();
        
        Assert.That(clientsList.Count(), Is.EqualTo(4));
        
        var clientNames = string.Join(",", clientsList.Select(a => a.Name).OrderBy(a => a));
        Assert.That(clientNames, Is.EqualTo("AllSettingsAndTypes,NoSettings,OneStringSetting,ThreeSettings"));
    }
}