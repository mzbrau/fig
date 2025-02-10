using System.Globalization;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Secrets;

public class SecretStoreHandler : ISecretStoreHandler
{
    private readonly ISecretStore _secretStore;
    private readonly IConfigurationRepository _configurationRepository;

    public SecretStoreHandler(ISecretStore secretStore, IConfigurationRepository configurationRepository)
    {
        _secretStore = secretStore;
        _configurationRepository = configurationRepository;
    }
    
    public async Task SaveSecrets(SettingClientBusinessEntity client)
    {
        if (!await UseSecretStore())
            return;
        
        await PersistSecrets(client);
        ClearSecretValues(client);
    }

    public async Task SaveSecrets(SettingClientBusinessEntity client, List<ChangedSetting> changes)
    {
        if (!await UseSecretStore())
            return;

        if (!changes.Any(a => a.IsSecret))
            return;

        var secretChanges = changes
            .Where(a => a.IsSecret)
            .Select(a => a.Name)
            .ToList();
        await PersistSecrets(client, secretChanges);
    }

    public async Task HydrateSecrets(SettingClientBusinessEntity client)
    {
        if (!await UseSecretStore())
            return;

        var secretKeys = client.Settings
            .Where(a => a.IsSecret)
            .Select(a => GetSecretKey(client, a.Name))
            .ToList();

        var secrets = await _secretStore.GetSecrets(secretKeys);

        foreach (var secret in secrets)
        {
            var setting = client.Settings.FirstOrDefault(a => GetSecretKey(client, a.Name) == secret.Key);
            if (setting is not null)
                setting.Value = new StringSettingBusinessEntity(secret.Value);
        }
    }

    public async Task ClearSecrets(SettingClientBusinessEntity client)
    {
        if (!await UseSecretStore())
            return;
        
        ClearSecretValues(client);
    }

    private void ClearSecretValues(SettingClientBusinessEntity client)
    {
        foreach (var setting in client.Settings.Where(a => a.IsSecret))
        {
            setting.Value = new StringSettingBusinessEntity(null);
        }
    }

    private async Task PersistSecrets(SettingClientBusinessEntity client)
    {
        var secrets = client.Settings
            .Where(a => a.IsSecret)
            .Where(a => a.Value?.GetValue() is not null)
            .Select(a =>
                new KeyValuePair<string, string>(
                    GetSecretKey(client, a.Name), 
                    Convert.ToString(a.DefaultValue?.GetValue(), CultureInfo.InvariantCulture) ?? string.Empty))
            .ToList();
        await _secretStore.PersistSecrets(secrets);
    }
    
    private async Task PersistSecrets(SettingClientBusinessEntity client, List<string> changedSecrets)
    {
        var secrets = client.Settings
            .Where(a => a.IsSecret)
            .Where(a => changedSecrets.Contains(a.Name))
            .Where(a => a.Value?.GetValue() is not null)
            .Select(a =>
                new KeyValuePair<string, string>(
                    GetSecretKey(client, a.Name), 
                    Convert.ToString(a.DefaultValue?.GetValue(), CultureInfo.InvariantCulture) ?? string.Empty))
            .ToList();
        await _secretStore.PersistSecrets(secrets);
    }
    
    private async Task<bool> UseSecretStore()
    {
        return (await _configurationRepository.GetConfiguration()).UseAzureKeyVault;
    }

    private string GetSecretKey(SettingClientBusinessEntity client, string settingName)
    {
        return $"fig-{client.Name.Replace(" ", "")}-" +
               $"{client.Instance?.Replace(" ", "")}-" +
               $"{settingName.Replace(" ", "")}";
    }
}