using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ApiStatusRepository : RepositoryBase<ApiStatusBusinessEntity>, IApiStatusRepository
{
    public ApiStatusRepository(ISession session) 
        : base(session)
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
        var criteria = Session.CreateCriteria<ApiStatusBusinessEntity>();
        criteria.Add(Restrictions.Eq("IsActive", true));
        criteria.SetLockMode(LockMode.Upgrade);
        return criteria.List<ApiStatusBusinessEntity>();
    }
}