using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.Configuration;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class ConfigurationService : AuthenticatedService, IConfigurationService
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IFigConfigurationConverter _figConfigurationConverter;

    public ConfigurationService(IConfigurationRepository configurationRepository, IEventLogRepository eventLogRepository, IEventLogFactory eventLogFactory, IFigConfigurationConverter figConfigurationConverter)
    {
        _configurationRepository = configurationRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _figConfigurationConverter = figConfigurationConverter;
    }
    
    public FigConfigurationDataContract GetConfiguration()
    {
        var configuration = _configurationRepository.GetConfiguration();
        return _figConfigurationConverter.Convert(configuration);
    }

    public void UpdateConfiguration(FigConfigurationDataContract configuration)
    {
        var currentConfiguration = _configurationRepository.GetConfiguration();
        var currentDataContract = _figConfigurationConverter.Convert(currentConfiguration);

        if (JsonConvert.SerializeObject(currentDataContract) == JsonConvert.SerializeObject(configuration))
        {
            return;
        }

        _eventLogRepository.Add(_eventLogFactory.ConfigurationChanged(currentDataContract, configuration, AuthenticatedUser));
        currentConfiguration.Update(configuration);
        _configurationRepository.UpdateConfiguration(currentConfiguration);
    }
}