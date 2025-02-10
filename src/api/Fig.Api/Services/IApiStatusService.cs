using Fig.Contracts.Status;

namespace Fig.Api.Services;

public interface IApiStatusService
{
    Task<List<ApiStatusDataContract>> GetAll();
}