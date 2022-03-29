using Fig.Web.Models.Authentication;

namespace Fig.Web.Facades;

public interface IUsersFacade
{
    Task LoadAllUsers();
    
    List<UserModel> UserCollection { get; }

    Task SaveUser(UserModel user);

    Task DeleteUser(UserModel user);

    Task AddUser(UserModel user);
}