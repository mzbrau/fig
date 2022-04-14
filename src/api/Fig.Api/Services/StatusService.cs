using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.Status;

namespace Fig.Api.Services;

public class StatusService : IStatusService
{
    private readonly IClientStatusRepository _clientStatusRepository;

    public StatusService(IClientStatusRepository clientStatusRepository)
    {
        _clientStatusRepository = clientStatusRepository;
    }

    public StatusResponseDataContract SyncStatus(
        string clientName, 
        string? instance, 
        string clientSecret,
        StatusRequestDataContract statusRequest)
    {
        var client = _clientStatusRepository.GetClient(clientName, instance);
        
        if (client == null)
            throw new KeyNotFoundException();

        if (!BCrypt.Net.BCrypt.EnhancedVerify(clientSecret, client.ClientSecret))
            throw new UnauthorizedAccessException();

        client.LastSeen = DateTime.UtcNow;
        client.LiveReload ??= statusRequest.LiveReload;
        client.PollIntervalSeconds ??= statusRequest.PollIntervalSeconds;
        client.UptimeSeconds = statusRequest.UptimeSeconds;
        
        _clientStatusRepository.UpdateClientStatus(client);

        return new StatusResponseDataContract
        {
            SettingUpdateAvailable = client.LastSettingValueUpdate > statusRequest.LastSettingUpdate,
            PollIntervalSeconds = client.PollIntervalSeconds ?? 30,
            LiveReload = client.LiveReload ?? true
        };
    }

    public void UpdateConfiguration(string clientName, string? instance, ClientConfigurationDataContract updatedConfiguration)
    {
        var client = _clientStatusRepository.GetClient(clientName, instance);
        
        if (client == null)
            throw new KeyNotFoundException();

        client.LiveReload = updatedConfiguration.LiveReload;
        client.PollIntervalSeconds = updatedConfiguration.PollIntervalSeconds;
        
        _clientStatusRepository.UpdateClientStatus(client);
    }
}