using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IUserRepository
{
    Task<UserBusinessEntity?> GetUser(string username, bool upgradeLock);
    
    Task<UserBusinessEntity?> GetUser(Guid id, bool upgradeLock);

    Task<Guid> SaveUser(UserBusinessEntity user);

    Task UpdateUser(UserBusinessEntity user);

    Task DeleteUser(UserBusinessEntity user);

    Task<IList<UserBusinessEntity>> GetAllUsers();
}