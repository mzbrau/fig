using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IClientRunSessionRepository
{
    Task<ClientRunSessionBusinessEntity?> GetRunSession(Guid id);

    Task UpdateRunSession(ClientRunSessionBusinessEntity runSession);

    Task DeleteRunSession(ClientRunSessionBusinessEntity runSession);
}