﻿using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Secrets;
using Fig.Contracts.Configuration;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class ConfigurationService : AuthenticatedService, IConfigurationService
{
    private readonly IConfigurationRepository _configurationRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly IFigConfigurationConverter _figConfigurationConverter;
    private readonly ISecretStore _secretStore;

    public ConfigurationService(IConfigurationRepository configurationRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        IFigConfigurationConverter figConfigurationConverter,
        ISecretStore secretStore)
    {
        _configurationRepository = configurationRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _figConfigurationConverter = figConfigurationConverter;
        _secretStore = secretStore;
    }
    
    public async Task<FigConfigurationDataContract> GetConfiguration()
    {
        var configuration = await _configurationRepository.GetConfiguration();
        return _figConfigurationConverter.Convert(configuration);
    }

    public async Task UpdateConfiguration(FigConfigurationDataContract configuration)
    {
        var currentConfiguration = await _configurationRepository.GetConfiguration(true);
        var currentDataContract = _figConfigurationConverter.Convert(currentConfiguration);

        if (JsonConvert.SerializeObject(currentDataContract) == JsonConvert.SerializeObject(configuration))
        {
            return;
        }

        await _eventLogRepository.Add(_eventLogFactory.ConfigurationChanged(currentDataContract, configuration, AuthenticatedUser));
        currentConfiguration.Update(configuration);
        await _configurationRepository.UpdateConfiguration(currentConfiguration);
    }

    public async Task<SecretStoreTestResultDataContract> TestAzureKeyVault()
    {
        return await _secretStore.PerformTest();
    }
}