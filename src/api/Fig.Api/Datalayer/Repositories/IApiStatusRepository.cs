using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IApiStatusRepository
{
    void AddOrUpdate(ApiStatusBusinessEntity status);

    IList<ApiStatusBusinessEntity> GetAllActive();
}