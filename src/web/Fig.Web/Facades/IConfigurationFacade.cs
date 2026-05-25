using Fig.Contracts.ApiSecret;
using Fig.Contracts.Configuration;
using Fig.Web.Models.Configuration;

namespace Fig.Web.Facades;

public interface IConfigurationFacade
{
    FigConfigurationModel ConfigurationModel { get; }
    
    long EventLogCount { get; }

    ApiSecretRotationStatusDataContract? ApiSecretRotationStatus { get; }

    Task LoadConfiguration();

    Task SaveConfiguration();
    
    Task MigrateEncryptedData();
    
    Task<SecretStoreTestResultDataContract> TestKeyVault();
}