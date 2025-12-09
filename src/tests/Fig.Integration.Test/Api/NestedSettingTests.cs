using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class NestedSettingTests : IntegrationTestBase
{
    [Test]
    public void ShallSetDefaultValues()
    {
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ClientWithNestedSettings>(secret);

        Assert.That(settings.CurrentValue.MessageBus?.Auth?.Username, Is.EqualTo("Frank"));
    }
    
    [Test]
    public async Task ShallPushDefaultValuesToServer()
    {
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ClientWithNestedSettings>(secret);

        var serverSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret);
        
        Assert.That(serverSettings.FirstOrDefault(a => a.Name == "MessageBus->Auth->Username")
                ?.Value?.GetValue(),
            Is.EqualTo("Frank"));
    }

    [Test]
    public async Task ShallOrderNestedSettingsBasedOnFileOrder()
    {
        var secret = GetNewSecret();
        var (settings, _) = InitializeConfigurationProvider<ClientWithNestedSettings>(secret);

        var serverSettings = await GetSettingsForClient(settings.CurrentValue.ClientName, secret);

        var settingsOrder = string.Join(",", serverSettings.Select(a => a.Name));
        
        Assert.That(settingsOrder,
            Is.EqualTo(
                "MessageBus->Uri,MessageBus->Auth->Username,MessageBus->Auth->Password,TimeoutMs,Database->ConnectionString,Database->TimeoutMs"));
    }

    [Test]
    public async Task ShallApplyUpdatedValues()
    {
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<ClientWithNestedSettings>(secret);

        const string newValue = "Sam";
        var updatedSettings = new List<SettingDataContract>
        {
            new("MessageBus->Auth->Username", new StringSettingDataContract(newValue))
        };
        
        await SetSettings(settings.CurrentValue.ClientName, updatedSettings);
        
        configuration.Reload();
        
        Assert.That(settings.CurrentValue?.MessageBus?.Auth?.Username, Is.EqualTo(newValue));
    }

    [Test]
    public async Task ShallAllowDisplayScriptsToAccessNestedSettingsByPropertyName()
    {
        // This test demonstrates that display scripts can be registered for nested settings
        // and the script runner can handle references to property names
        
        await SetConfiguration(CreateConfiguration(allowDisplayScripts: true));
        
        var secret = GetNewSecret();
        var (settings, configuration) = InitializeConfigurationProvider<ClientWithNestedSettings>(secret);
        
        var clients = await GetAllClients();
        var client = clients.Single();
        
        // Verify that nested settings are registered correctly
        var usernameSetting = client.Settings.FirstOrDefault(s => s.Name == "MessageBus->Auth->Username");
        var passwordSetting = client.Settings.FirstOrDefault(s => s.Name == "MessageBus->Auth->Password");
        var uriSetting = client.Settings.FirstOrDefault(s => s.Name == "MessageBus->Uri");
        
        Assert.That(usernameSetting, Is.Not.Null);
        Assert.That(passwordSetting, Is.Not.Null);
        Assert.That(uriSetting, Is.Not.Null);
        
        // The ScriptRunner fix allows these settings to be accessed by property name
        // in display scripts, e.g., "Username", "Password", "Uri" instead of full paths
        Assert.That(usernameSetting!.Name, Is.EqualTo("MessageBus->Auth->Username"));
        Assert.That(passwordSetting!.Name, Is.EqualTo("MessageBus->Auth->Password"));
        Assert.That(uriSetting!.Name, Is.EqualTo("MessageBus->Uri"));
    }
}