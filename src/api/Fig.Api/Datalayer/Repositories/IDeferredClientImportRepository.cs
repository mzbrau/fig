using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IDeferredClientImportRepository
{
    DeferredClientImportBusinessEntity? GetClient(string name, string? instance);

    void SaveClient(DeferredClientImportBusinessEntity client);

    void DeleteClient(string clientName, string? instance);

    IEnumerable<DeferredClientImportBusinessEntity> GetAllClients();
}