using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IUserRepository
{
    UserBusinessEntity? GetUser(string username, bool upgradeLock);
    
    UserBusinessEntity? GetUser(Guid id, bool upgradeLock);

    Guid SaveUser(UserBusinessEntity user);

    void UpdateUser(UserBusinessEntity user);

    void DeleteUser(UserBusinessEntity user);

    IList<UserBusinessEntity> GetAllUsers();
}