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

    public async Task<UserBusinessEntity?> GetUser(string username, bool upgradeLock)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<UserBusinessEntity>();
        if (upgradeLock)
            criteria.SetLockMode(LockMode.Upgrade);
        
        criteria.Add(Restrictions.Eq("Username", username));
        return await criteria.UniqueResultAsync<UserBusinessEntity>();
    }

    public async Task<UserBusinessEntity?> GetUser(Guid id, bool upgradeLock)
    {
        return await Get(id, upgradeLock);
    }

    public async Task<Guid> SaveUser(UserBusinessEntity user)
    {
        return await Save(user);
    }

    public async Task UpdateUser(UserBusinessEntity user)
    {
        await Update(user);
    }

    public async Task DeleteUser(UserBusinessEntity user)
    {
        await Delete(user);
    }

    public async Task<IList<UserBusinessEntity>> GetAllUsers()
    {
        return await GetAll(false);
    }
}