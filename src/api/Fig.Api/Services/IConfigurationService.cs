using Fig.Contracts.Configuration;

namespace Fig.Api.Services;

public interface IConfigurationService : IAuthenticatedService
{
    FigConfigurationDataContract GetConfiguration();

    void UpdateConfiguration(FigConfigurationDataContract configuration);
    
    Task<SecretStoreTestResultDataContract> TestAzureKeyVault();
}