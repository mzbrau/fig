using Fig.Contracts.Status;

namespace Fig.Api.Services;

public interface IStatusService : IAuthenticatedService
{
    Task<StatusResponseDataContract> SyncStatus(
        string clientName,
        string? instance,
        string clientSecret,
        StatusRequestDataContract statusRequest);

    Task SetLiveReload(Guid runSessionId, bool liveReload);

    Task<List<ClientStatusDataContract>> GetAll();

    Task<List<CustomStatusSessionPropertiesDataContract>> GetCustomProperties();

    Task<List<CustomStatusSessionPropertiesDataContract>> GetCustomProperties(string clientName, string? instance);

    void SetRequesterDetails(string? ipAddress, string? hostname);
    
    Task RequestRestart(Guid runSessionId);

    Task MarkRestartRequired(string clientName, string? instance);
}