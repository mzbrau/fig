using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Fig.Common.Constants;
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
        SecretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Never);
    }

    [Test]
    public async Task ShallPersistSecretsInAzureOnNewRegistrationWhenEnabled()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        await RegisterSettings<SecretSettings>();
        SecretStoreMock.Verify(a => a.PersistSecrets(It.Is<List<KeyValuePair<string, string>>>(x => x.Count == 1 & x[0].Key.Contains("SecretWithDefault"))), Times.Once);
    }

    [Test]
    public async Task ShallNotPersistSecretsInAzureOnUpdatedRegistrationWhenDisabled()
    {
        var secret = GetNewSecret();
        await RegisterSettings<SecretSettings>(secret);
        await RegisterSettings<SecretSettingsWithExtraSecret>(secret);
        SecretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Never);
    }
    
    [Test]
    public async Task ShallPersistSecretsInAzureOnUpdatedRegistrationWhenEnabled()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        var secret = GetNewSecret();
        await RegisterSettings<SecretSettings>(secret);
        await RegisterSettings<SecretSettingsWithExtraSecret>(secret);
        SecretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Exactly(2));
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
        
        SecretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Never);
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
        
        SecretStoreMock.Verify(a => a.PersistSecrets(It.IsAny<List<KeyValuePair<string, string>>>()), Times.Exactly(2));
    }

    [Test]
    public async Task ShallNotRequestSecretsFromAzureOnClientSettingsRequestWhenDisabled()
    {
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await GetSettingsForClient(client.ClientName, secret);
        
        SecretStoreMock.Verify(a => a.GetSecrets(It.IsAny<List<string>>()), Times.Never);
    }
    
    [Test]
    public async Task ShallRequestSecretsFromAzureOnClientSettingsRequestWhenEnabled()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await GetSettingsForClient(client.ClientName, secret);
        
        SecretStoreMock.Verify(a => a.GetSecrets(It.IsAny<List<string>>()), Times.Exactly(1));
    }

    [Test]
    public async Task ShallNotRequestSecretsFromAzureWhenLoadingAllClients()
    {
        await SetConfiguration(CreateConfiguration(useAzureKeyVault: true));
        await RegisterSettings<SecretSettings>();
        await GetAllClients();
        
        SecretStoreMock.Verify(a => a.GetSecrets(It.IsAny<List<string>>()), Times.Never);
    }

    [Test]
    public async Task ShallReturnDataGridSecretsInPlainTextToClients()
    {
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await SetSettings(client.ClientName, new List<SettingDataContract>()
        {
            new(nameof(client.Logins), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user1" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "abc123" }
                },
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user2" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "xyz456" }
                }
            }))
        });

        var settings = await GetSettingsForClient(client.ClientName, secret);

        var loginSetting = settings.FirstOrDefault(a => a.Name == nameof(client.Logins))?.Value as DataGridSettingDataContract;
        Assert.That(loginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("abc123"));
        Assert.That(loginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("xyz456"));
    }
    
    [Test]
    public async Task ShallReturnDataGridSecretsWithPlaceholderInWebApp()
    {
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await SetSettings(client.ClientName, new List<SettingDataContract>()
        {
            new(nameof(client.Logins), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user1" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "abc123" }
                },
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user2" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "xyz456" }
                }
            }))
        });

        var webAppClient = (await GetAllClients()).First();
        
        var loginSetting = webAppClient.Settings.FirstOrDefault(a => a.Name == nameof(client.Logins))?.Value as DataGridSettingDataContract;
        Assert.That(loginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(SecretConstants.SecretPlaceholder));
        Assert.That(loginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(SecretConstants.SecretPlaceholder));
    }

    [Test]
    public async Task ShallHandleDefaultValuesForSecretsInDataGrids()
    {
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);

        var settings = await GetSettingsForClient(client.ClientName, secret);

        var clientLoginSetting = settings.FirstOrDefault(a => a.Name == nameof(client.LoginsWithDefault))?.Value as DataGridSettingDataContract;
        Assert.That(clientLoginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("myPassword"));
        Assert.That(clientLoginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("myPassword2"));
        
        var webAppClient = (await GetAllClients()).First();
        
        var webLoginSetting = webAppClient.Settings.FirstOrDefault(a => a.Name == nameof(client.LoginsWithDefault))?.Value as DataGridSettingDataContract;
        Assert.That(webLoginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(SecretConstants.SecretPlaceholder));
        Assert.That(webLoginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(SecretConstants.SecretPlaceholder));
    }

    [Test]
    public async Task ShallCorrectlyChooseWhenToRedactSecretsEvenAfterMultipleUpdates()
    {
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);

        await SetSettings(client.ClientName, new List<SettingDataContract>()
        {
            new(nameof(client.Logins), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user1" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "abc123" },
                    { nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret), null }
                },
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user2" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "xyz456" },
                    { nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret), null }
                }
            }))
        });
        
        await SetSettings(client.ClientName, new List<SettingDataContract>()
        {
            new(nameof(client.Logins), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user1" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "abc123" },
                    { nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret), "poiuy" }
                }
            }))
        });
        
        await SetSettings(client.ClientName, new List<SettingDataContract>()
        {
            new(nameof(client.Logins), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user12" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "abc123" },
                    { nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret), null }
                },
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user2" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "qwerty" },
                    { nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret), "snap" }
                }
            }))
        });

        var webAppClient = (await GetAllClients()).First();
        
        var webLoginSetting = webAppClient.Settings.FirstOrDefault(a => a.Name == nameof(client.Logins))?.Value as DataGridSettingDataContract;
        Assert.That(webLoginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(SecretConstants.SecretPlaceholder));
        Assert.That(webLoginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo(SecretConstants.SecretPlaceholder));
        Assert.That(webLoginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret)], Is.Null);
        Assert.That(webLoginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret)], Is.EqualTo(SecretConstants.SecretPlaceholder));
        
        var settings = await GetSettingsForClient(client.ClientName, secret);

        var clientLoginSetting = settings.FirstOrDefault(a => a.Name == nameof(client.Logins))?.Value as DataGridSettingDataContract;
        Assert.That(clientLoginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("abc123"));
        Assert.That(clientLoginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("qwerty"));
        Assert.That(clientLoginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret)], Is.Null);
        Assert.That(clientLoginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret)], Is.EqualTo("snap"));
    }

    [Test]
    public async Task ShallOnlyUpdateSecretValueIfPlaceholderHasBeenChanged()
    {
        var secret = GetNewSecret();
        var client = await RegisterSettings<SecretSettings>(secret);
        await SetSettings(client.ClientName, new List<SettingDataContract>()
        {
            new(nameof(client.LoginsWithDefault), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user1" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), SecretConstants.SecretPlaceholder },
                    { nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret), "snap" }
                },
                new()
                {
                    { nameof(Fig.Test.Common.TestSettings.Login.Username), "user3" },
                    { nameof(Fig.Test.Common.TestSettings.Login.Password), "newPassword" },
                    { nameof(Fig.Test.Common.TestSettings.Login.AnotherSecret), "snap" }
                }
            }))
        });

        var settings = await GetSettingsForClient(client.ClientName, secret);

        var loginSetting = settings.FirstOrDefault(a => a.Name == nameof(client.LoginsWithDefault))?.Value as DataGridSettingDataContract;
        Assert.That(loginSetting?.Value?[0][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("myPassword"));
        Assert.That(loginSetting?.Value?[1][nameof(Fig.Test.Common.TestSettings.Login.Password)], Is.EqualTo("newPassword"));
    }
}