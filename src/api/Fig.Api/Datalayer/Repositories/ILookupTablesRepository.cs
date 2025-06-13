using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ILookupTablesRepository
{
    Task<IList<LookupTableBusinessEntity>> GetAllItems();

    Task<LookupTableBusinessEntity?> GetItem(Guid id);
    
    Task<LookupTableBusinessEntity?> GetItemByName(string name);

    Task SaveItem(LookupTableBusinessEntity item);

    Task UpdateItem(LookupTableBusinessEntity item);

    Task DeleteItem(LookupTableBusinessEntity item);
}