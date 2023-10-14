using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Moq;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

[TestFixture]
public class SecretHandlingTests : IntegrationTestBase
{
    [Test]
    public async Task ShallNotPersistSecretsInAzureOnNewRegistrationWhenDisabled()
    {
        await RegisterSettings<SecretSettings>();
        secretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Never);
    }

    [Test]
    public async Task ShallPersistSecretsInAzureOnNewRegistrationWhenEnabled()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        await RegisterSettings<SecretSettings>();
        secretStoreMock.Verify(a => a.PersistSecrets(It.Is<List<KeyValuePair<string, string>>>(x => x.Count == 1 & x[0].Key.Contains("SecretWithDefault"))), Times.Once);
    }

    [Test]
    public async Task ShallNotPersistSecretsInAzureOnUpdatedRegistrationWhenDisabled()
    {
        var secret = GetNewSecret();
        await RegisterSettings<SecretSettings>(secret);
        await RegisterSettings<SecretSettingsWithExtraSecret>(secret);
        secretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Never);
    }
    
    [Test]
    public async Task ShallPersistSecretsInAzureOnUpdatedRegistrationWhenEnabled()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        var secret = GetNewSecret();
        await RegisterSettings<SecretSettings>(secret);
        await RegisterSettings<SecretSettingsWithExtraSecret>(secret);
        secretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Exactly(2));
    }

    [Test]
    public async Task ShallNotPersistSecretsInAzureOnSettingUpdateWhenDisabled()
    {
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await SetSettings(client.ClientName, new List<SettingDataContract>()
        {
            new(nameof(client.SecretNoDefault), new StringSettingDataContract("some val"))
        });
        
        secretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Never);
    }
    
    [Test]
    public async Task ShallPersistSecretsInAzureOnSettingUpdateWhenEnabled()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await SetSettings(client.ClientName, new List<SettingDataContract>()
        {
            new(nameof(client.SecretNoDefault), new StringSettingDataContract("some val"))
        });
        
        secretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Exactly(2));
    }

    [Test]
    public async Task ShallNotRequestSecretsFromAzureOnClientSettingsRequestWhenDisabled()
    {
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await GetSettingsForClient(client.ClientName, secret);
        
        secretStoreMock.Verify(a => a.GetSecrets(It.IsAny<List<string>>()), Times.Never);
    }
    
    [Test]
    public async Task ShallRequestSecretsFromAzureOnClientSettingsRequestWhenEnabled()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await GetSettingsForClient(client.ClientName, secret);
        
        secretStoreMock.Verify(a => a.GetSecrets(It.IsAny<List<string>>()), Times.Exactly(1));
    }

    [Test]
    public async Task ShallNotRequestSecretsFromAzureWhenLoadingAllClients()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        await RegisterSettings<SecretSettings>();
        await GetAllClients();
        
        secretStoreMock.Verify(a => a.GetSecrets(It.IsAny<List<string>>()), Times.Never);
    }
}