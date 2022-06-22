using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.Status;

namespace Fig.Api.Services;

public class ApiStatusService : IApiStatusService
{
    private readonly IApiStatusRepository _apiStatusRepository;
    private readonly IApiStatusConverter _apiStatusConverter;

    public ApiStatusService(IApiStatusRepository apiStatusRepository, IApiStatusConverter apiStatusConverter)
    {
        _apiStatusRepository = apiStatusRepository;
        _apiStatusConverter = apiStatusConverter;
    }
    
    public List<ApiStatusDataContract> GetAll()
    {
        var items = _apiStatusRepository.GetAllActive();
        return _apiStatusConverter.Convert(items);
    }
}