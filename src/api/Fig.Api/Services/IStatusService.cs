using Fig.Contracts.Status;

namespace Fig.Api.Services;

public interface IStatusService : IAuthenticatedService
{
    Task<StatusResponseDataContract> SyncStatus(
        string clientName,
        string? instance,
        string clientSecret,
        StatusRequestDataContract statusRequest);

    void SetLiveReload(Guid runSessionId, bool liveReload);

    List<ClientStatusDataContract> GetAll();

    void SetRequesterDetails(string? ipAddress, string? hostname);
    
    void RequestRestart(Guid runSessionId);

    void MarkRestartRequired(string clientName, string? instance);
}