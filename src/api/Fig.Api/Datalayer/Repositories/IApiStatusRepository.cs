using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IApiStatusRepository
{
    Task AddOrUpdate(ApiStatusBusinessEntity status);

    Task<IList<ApiStatusBusinessEntity>> GetAllActive();
}