using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class ApiStatusRepository : RepositoryBase<ApiStatusBusinessEntity>, IApiStatusRepository
{
    public ApiStatusRepository(IFigSessionFactory sessionFactory) 
        : base(sessionFactory)
    {
    }

    public void AddOrUpdate(ApiStatusBusinessEntity status)
    {
        if (status.Id == null)
        {
            Save(status);
        }
        else
        {
            Update(status);
        }
    }

    public IList<ApiStatusBusinessEntity> GetAllActive()
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<ApiStatusBusinessEntity>();
        criteria.Add(Restrictions.Eq("IsActive", true));
        return criteria.List<ApiStatusBusinessEntity>();
    }
}