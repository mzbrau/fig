using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IDeferredClientImportRepository
{
    Task<IList<DeferredClientImportBusinessEntity>> GetClients(string name);

    Task AddClient(DeferredClientImportBusinessEntity client);

    Task DeleteClient(Guid id);

    Task<IList<DeferredClientImportBusinessEntity>> GetAllClients(UserDataContract? requestingUser);

    Task DeleteAll();
}