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
}