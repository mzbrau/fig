using Fig.Contracts.Status;

namespace Fig.Api.Services;

public interface IStatusService : IAuthenticatedService
{
    Task<StatusResponseDataContract> SyncStatus(
        string clientName,
        string? instance,
        string clientSecret,
        StatusRequestDataContract statusRequest);

    ClientConfigurationDataContract UpdateConfiguration(
        string clientName,
        string? instance,
        ClientConfigurationDataContract updatedConfiguration);

    List<ClientStatusDataContract> GetAll();

    void SetRequesterDetails(string? ipAddress, string? hostname);
}