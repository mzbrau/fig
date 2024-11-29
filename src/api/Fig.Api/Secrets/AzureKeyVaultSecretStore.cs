using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.Configuration;

namespace Fig.Api.Secrets;

public class AzureKeyVaultSecretStore : ISecretStore
{
    private const string KeyVaultUrl = "https://{0}.vault.azure.net";
    private readonly IConfigurationRepository _configurationRepository;
    private readonly ILogger<AzureKeyVaultSecretStore> _logger;

    public AzureKeyVaultSecretStore(IConfigurationRepository configurationRepository, ILogger<AzureKeyVaultSecretStore> logger)
    {
        _configurationRepository = configurationRepository;
        _logger = logger;
    }
    
    public async Task<SecretStoreTestResultDataContract> PerformTest()
    {
        try
        {
            return await PerformTestInternal();
        }
        catch (Exception ex)
        {
            return new SecretStoreTestResultDataContract(false, ex.Message);
        }
    }

    public async Task PersistSecrets(List<KeyValuePair<string, string>> secrets)
    {
        try
        {
            var client = GetClient();

            foreach (var secret in secrets)
            {
                await client.SetSecretAsync(secret.Key, secret.Value);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to persist secrets in Azure Key Vault");
        }
    }

    public async Task<List<KeyValuePair<string, string>>> GetSecrets(List<string> keys)
    {
        try
        {
            var client = GetClient();
            var results = new List<KeyValuePair<string, string>>();

            foreach (var key in keys)
            {
                var value = await client.GetSecretAsync(key);
                results.Add(new KeyValuePair<string, string>(key, value.Value.Value));
            }

            return results;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Unable to get secrets from Azure Key Vault");
        }

        return new List<KeyValuePair<string, string>>();
    }


    private async Task<SecretStoreTestResultDataContract> PerformTestInternal()
    {
        const string fakeSecretName = "FigTestSecret";
        const string fakeSecretValue = "SecretValue";

        var keyVaultName = GetKeyVaultName();

        if (keyVaultName is null)
            return new SecretStoreTestResultDataContract(false, "Key Vault name was not set");

        var kvUri = string.Format(KeyVaultUrl, keyVaultName);

        var client = new SecretClient(new Uri(kvUri), new DefaultAzureCredential());

        await client.SetSecretAsync(fakeSecretName, fakeSecretValue);
        await client.GetSecretAsync(fakeSecretName);

        return new SecretStoreTestResultDataContract(true, "Key Vault configured correctly");
    }

    private SecretClient GetClient()
    {
        var keyVaultName = GetKeyVaultName();

        if (keyVaultName is null)
        {
            _logger.LogError("Empty key vault name. Unable to persist");
            throw new ArgumentException("Key Vault Name was not set");
        }

        var kvUri = string.Format(KeyVaultUrl, keyVaultName);
        return new SecretClient(new Uri(kvUri), new DefaultAzureCredential());
    }

    private string? GetKeyVaultName()
    {
        return _configurationRepository.GetConfiguration().AzureKeyVaultName;
    }
}