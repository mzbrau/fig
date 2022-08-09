using Fig.Contracts.Common;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ILookupTablesRepository
{
    IEnumerable<LookupTableBusinessEntity> GetAllItems();

    LookupTableBusinessEntity? GetItem(Guid id);

    LookupTableBusinessEntity? GetItem(String name);

    void SaveItem(LookupTableBusinessEntity item);

    void UpdateItem(LookupTableBusinessEntity item);

    void DeleteItem(LookupTableBusinessEntity item);
}