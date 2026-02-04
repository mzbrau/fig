using System.Diagnostics;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.ClientRegistrationHistory;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class ClientRegistrationHistoryService : IClientRegistrationHistoryService
{
    private readonly IClientRegistrationHistoryRepository _repository;
    private readonly ILogger<ClientRegistrationHistoryService> _logger;

    public ClientRegistrationHistoryService(
        IClientRegistrationHistoryRepository repository,
        ILogger<ClientRegistrationHistoryService> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    public async Task RecordRegistration(SettingsClientDefinitionDataContract client)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        var settings = client.Settings.Select(s => new SettingDefaultValueDataContract(
            s.Name,
            GetDefaultValueAsString(s),
            s.Advanced
        )).ToList();

        var history = new ClientRegistrationHistoryBusinessEntity
        {
            RegistrationDateUtc = DateTime.UtcNow,
            ClientName = client.Name,
            ClientVersion = client.ClientVersion ?? string.Empty,
            SettingsJson = JsonConvert.SerializeObject(settings, JsonSettings.FigDefault)
        };

        await _repository.Add(history);
        _logger.LogInformation("Recorded registration history for client {ClientName} version {ClientVersion}",
            client.Name.Sanitize(), client.ClientVersion);
    }

    public async Task<ClientRegistrationHistoryCollectionDataContract> GetAllHistory()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        var historyEntities = await _repository.GetAll();
        var registrations = historyEntities
            .Select(ConvertToDataContract)
            .GroupBy(r => r.ClientName)
            .SelectMany(g => g
                .OrderByDescending(r => r.RegistrationDateUtc)
                .Take(3))
            .OrderBy(r => r.ClientName)
            .ThenBy(r => r.RegistrationDateUtc)
            .ToList();

        return new ClientRegistrationHistoryCollectionDataContract(registrations);
    }

    public async Task ClearHistory()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        await _repository.ClearAll();
        _logger.LogInformation("Cleared all client registration history");
    }

    private static ClientRegistrationHistoryDataContract ConvertToDataContract(ClientRegistrationHistoryBusinessEntity entity)
    {
        var settings = string.IsNullOrEmpty(entity.SettingsJson)
            ? new List<SettingDefaultValueDataContract>()
            : JsonConvert.DeserializeObject<List<SettingDefaultValueDataContract>>(entity.SettingsJson, JsonSettings.FigDefault)
              ?? new List<SettingDefaultValueDataContract>();

        return new ClientRegistrationHistoryDataContract(
            entity.Id,
            entity.RegistrationDateUtc,
            entity.ClientName,
            entity.ClientVersion,
            settings);
    }

    private static string? GetDefaultValueAsString(SettingDefinitionDataContract setting)
    {
        if (setting.DefaultValue == null)
            return null;

        try
        {
            return JsonConvert.SerializeObject(setting.DefaultValue.GetValue(), JsonSettings.FigDefault);
        }
        catch
        {
            return setting.DefaultValue.ToString();
        }
    }
}
