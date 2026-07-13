using Fig.Api.Datalayer.Repositories;
using Fig.Api.Exceptions;
using Fig.Api.ExtensionMethods;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public sealed class ClientOverrideService : IClientOverrideService
{
    private readonly ISettingClientRepository _settingClientRepository;
    private readonly ISettingHistoryRepository _settingHistoryRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;

    public ClientOverrideService(
        ISettingClientRepository settingClientRepository,
        ISettingHistoryRepository settingHistoryRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory)
    {
        _settingClientRepository = settingClientRepository;
        _settingHistoryRepository = settingHistoryRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
    }

    public async Task<SettingClientBusinessEntity> CreateClientOverride(
        string clientName,
        string instance,
        UserDataContract? authenticatedUser)
    {
        if (string.IsNullOrWhiteSpace(instance))
            throw new ArgumentException("Instance must be provided for override creation.", nameof(instance));

        var nonOverrideClient = await _settingClientRepository.GetClient(clientName);
        if (nonOverrideClient == null)
            throw new UnknownClientException(clientName);

        var client = nonOverrideClient.CreateOverride(instance);
        await _settingClientRepository.RegisterClient(client);

        await _eventLogRepository.Add(
            _eventLogFactory.InstanceOverrideCreated(client.Id, clientName, instance, authenticatedUser));

        await CloneSettingHistory(nonOverrideClient, client);
        return client;
    }

    private async Task CloneSettingHistory(
        SettingClientBusinessEntity originalClient,
        SettingClientBusinessEntity instanceClient)
    {
        foreach (var setting in originalClient.Settings)
        {
            var history = await _settingHistoryRepository.GetAll(originalClient.Id, setting.Name);
            foreach (var historyItem in history)
            {
                await _settingHistoryRepository.Add(historyItem.Clone(instanceClient.Id));
            }
        }
    }
}

