using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class UserRepository : RepositoryBase<UserBusinessEntity>, IUserRepository
{
    public UserRepository(IFigSessionFactory sessionFactory)
        : base(sessionFactory)
    {
    }

    public UserBusinessEntity? GetUser(string username)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<UserBusinessEntity>();
        criteria.Add(Restrictions.Eq("Username", username));
        return criteria.UniqueResult<UserBusinessEntity>();
    }

    public UserBusinessEntity? GetUser(Guid id)
    {
        return Get(id);
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

    public IEnumerable<UserBusinessEntity> GetAllUsers()
    {
        return GetAll();
    }
}