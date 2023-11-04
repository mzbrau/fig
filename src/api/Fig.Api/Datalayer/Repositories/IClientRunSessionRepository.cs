using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IClientRunSessionRepository
{
    ClientRunSessionBusinessEntity? GetRunSession(Guid id);
    
    void UpdateRunSession(ClientRunSessionBusinessEntity runSession);
}