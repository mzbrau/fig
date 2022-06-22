using Fig.Contracts.Status;

namespace Fig.Api.Services;

public interface IApiStatusService
{
    List<ApiStatusDataContract> GetAll();
}