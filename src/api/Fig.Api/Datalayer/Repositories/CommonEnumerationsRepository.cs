using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class CommonEnumerationsRepository : RepositoryBase<CommonEnumerationBusinessEntity>, ICommonEnumerationsRepository
{
    public CommonEnumerationsRepository(IFigSessionFactory sessionFactory) 
        : base(sessionFactory)
    {
    }

    public CommonEnumerationBusinessEntity? GetItem(Guid id)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<CommonEnumerationBusinessEntity>();
        criteria.Add(Restrictions.Eq("Id", id));
        var item = criteria.UniqueResult<CommonEnumerationBusinessEntity>();
        return item;
    }

    public IEnumerable<CommonEnumerationBusinessEntity> GetAllItems()
    {
        return GetAll();
    }

    public void SaveItem(CommonEnumerationBusinessEntity item)
    {
        Save(item);
    }

    public void UpdateItem(CommonEnumerationBusinessEntity item)
    {
        Update(item);
    }

    public void DeleteItem(CommonEnumerationBusinessEntity item)
    {
        Delete(item);
    }
} 