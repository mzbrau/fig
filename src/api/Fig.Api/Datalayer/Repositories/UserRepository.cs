using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class UserRepository : RepositoryBase<UserBusinessEntity>, IUserRepository
{
    public UserRepository(ISession session)
        : base(session)
    {
    }

    public UserBusinessEntity? GetUser(string username, bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<UserBusinessEntity>();
        if (upgradeLock)
            criteria.SetLockMode(LockMode.Upgrade);
        
        criteria.Add(Restrictions.Eq("Username", username));
        return criteria.UniqueResult<UserBusinessEntity>();
    }

    public UserBusinessEntity? GetUser(Guid id, bool upgradeLock)
    {
        return Get(id, upgradeLock);
    }

    public Guid SaveUser(UserBusinessEntity user)
    {
        return Save(user);
    }

    public void UpdateUser(UserBusinessEntity user)
    {
        Update(user);
    }

    public void DeleteUser(UserBusinessEntity user)
    {
        Delete(user);
    }

    public IList<UserBusinessEntity> GetAllUsers()
    {
        return GetAll(false);
    }
}