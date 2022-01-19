using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IUserRepository
{
    UserBusinessEntity? GetUser(string username);
    
    UserBusinessEntity? GetUser(Guid id);

    Guid SaveUser(UserBusinessEntity user);

    void UpdateUser(UserBusinessEntity user);

    void DeleteUser(UserBusinessEntity user);

    IEnumerable<UserBusinessEntity> GetAllUsers();
}