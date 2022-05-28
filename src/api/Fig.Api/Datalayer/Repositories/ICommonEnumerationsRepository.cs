using Fig.Contracts.Common;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ICommonEnumerationsRepository
{
    IEnumerable<CommonEnumerationBusinessEntity> GetAllItems();

    CommonEnumerationBusinessEntity? GetItem(Guid id);
    
    void SaveItem(CommonEnumerationBusinessEntity item);

    void UpdateItem(CommonEnumerationBusinessEntity item);

    void DeleteItem(CommonEnumerationBusinessEntity item);
}