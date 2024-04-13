using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IDeferredClientImportRepository
{
    IEnumerable<DeferredClientImportBusinessEntity> GetClients(string name, string? instance);

    void AddClient(DeferredClientImportBusinessEntity client);

    void DeleteClient(Guid id);

    IEnumerable<DeferredClientImportBusinessEntity> GetAllClients(UserDataContract? requestingUser);
}