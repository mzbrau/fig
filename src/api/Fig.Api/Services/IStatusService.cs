using Fig.Contracts.Status;

namespace Fig.Api.Services;

public interface IStatusService
{
    StatusResponseDataContract SyncStatus(
        string clientName, 
        string? instance, 
        string clientSecret,
        StatusRequestDataContract statusRequest);

    void UpdateConfiguration(
        string clientName, 
        string? instance,
        ClientConfigurationDataContract updatedConfiguration);
}