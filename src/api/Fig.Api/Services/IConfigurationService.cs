using Fig.Contracts.Configuration;

namespace Fig.Api.Services;

public interface IConfigurationService : IAuthenticatedService
{
    Task<FigConfigurationDataContract> GetConfiguration();

    Task UpdateConfiguration(FigConfigurationDataContract configuration);
    
    Task<SecretStoreTestResultDataContract> TestAzureKeyVault();
}