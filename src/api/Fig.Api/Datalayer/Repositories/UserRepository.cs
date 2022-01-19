using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class UserRepository : RepositoryBase<UserBusinessEntity>, IUserRepository
{
    public UserRepository(IFigSessionFactory sessionFactory)
        : base(sessionFactory)
    {
        AddDefaultAdminUser();
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
    
    // Add default admin user if none exists in the database.
    private void AddDefaultAdminUser()
    {
        var users = GetAllUsers();

        if (!users.Any())
        {
            var defaultUser = new UserBusinessEntity
            {
                Username = "admin",
                FirstName = "Default",
                LastName = "User",
                Role = Role.Administrator,
                PasswordHash = BCrypt.Net.BCrypt.EnhancedHashPassword("admin")
            };
            SaveUser(defaultUser);
        }
    }
}