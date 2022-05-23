using Fig.Contracts.Configuration;

namespace Fig.Api.Services;

public interface IConfigurationService
{
    FigConfigurationDataContract GetConfiguration();
    
    void UpdateConfiguration(FigConfigurationDataContract configuration);
}