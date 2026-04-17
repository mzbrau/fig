using System.Linq;
using System.Threading.Tasks;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class DisplayScriptTests : IntegrationTestBase
{
    [Test]
    public async Task ShallRegisterDisplayScript()
    {
        await SetConfiguration(CreateConfiguration(allowDisplayScripts: true));
        await RegisterSettings<ThreeSettings>();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(
            clients.First().Settings.FirstOrDefault(a => a.Name == nameof(ThreeSettings.ABoolSetting))?.DisplayScript,
            Is.EqualTo(ThreeSettings.DisplayScript));
    }

    [Test]
    public async Task ShallNotReturnDisplayScriptWhenDisabled()
    {
        await SetConfiguration(CreateConfiguration(allowDisplayScripts: false));
        await RegisterSettings<ThreeSettings>();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(
            clients.First().Settings.FirstOrDefault(a => a.Name == nameof(ThreeSettings.ABoolSetting))?.DisplayScript,
            Is.Null);
    }

    [Test]
    public async Task ShallUpdateDisplayScript()
    {
        var secret = GetNewSecret();
        await SetConfiguration(CreateConfiguration(allowDisplayScripts: true));
        await RegisterSettings<ThreeSettings>(secret);
        await RegisterSettings<ThreeSettingsUpdatedScript>(secret);

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
        Assert.That(
            clients.First().Settings.FirstOrDefault(a => a.Name == nameof(ThreeSettings.ABoolSetting))?.DisplayScript,
            Is.EqualTo(ThreeSettingsUpdatedScript.DisplayScript));
        
    }

    [Test]
    public async Task ShallRegisterMultipleDisplayScripts()
    {
        await SetConfiguration(CreateConfiguration(allowDisplayScripts: true));
        await RegisterSettings<MultipleDisplayScriptsSettings>();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
        var displayScript = clients.First().Settings
            .FirstOrDefault(a => a.Name == nameof(MultipleDisplayScriptsSettings.ABoolSetting))?.DisplayScript;
        Assert.That(displayScript, Is.Not.Null);
        Assert.That(displayScript, Does.Contain(MultipleDisplayScriptsSettings.Script1));
        Assert.That(displayScript, Does.Contain(MultipleDisplayScriptsSettings.Script2));
    }

    [Test]
    public async Task ShallSubstitutePlaceholderInDisplayScript()
    {
        await SetConfiguration(CreateConfiguration(allowDisplayScripts: true));
        await RegisterSettings<MultipleDisplayScriptsSettings>();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
        var displayScript = clients.First().Settings
            .FirstOrDefault(a => a.Name == nameof(MultipleDisplayScriptsSettings.AStringSetting))?.DisplayScript;
        Assert.That(displayScript, Is.Not.Null);
        Assert.That(displayScript, Does.Not.Contain("{{this}}"));
        Assert.That(displayScript, Does.Contain(nameof(MultipleDisplayScriptsSettings.AStringSetting)));
    }

    [Test]
    public async Task ShallRegisterLibraryDisplayScript()
    {
        await SetConfiguration(CreateConfiguration(allowDisplayScripts: true));
        await RegisterSettings<MultipleDisplayScriptsSettings>();

        var clients = (await GetAllClients()).ToList();

        Assert.That(clients.Count, Is.EqualTo(1));
        var displayScript = clients.First().Settings
            .FirstOrDefault(a => a.Name == nameof(MultipleDisplayScriptsSettings.AnIntSetting))?.DisplayScript;
        Assert.That(displayScript, Is.Not.Null);
        Assert.That(displayScript, Does.Not.Contain("{{this}}"));
        Assert.That(displayScript, Does.Contain($"{nameof(MultipleDisplayScriptsSettings.AnIntSetting)}.Value"));
    }
}
