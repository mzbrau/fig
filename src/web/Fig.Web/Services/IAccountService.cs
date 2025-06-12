using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;

namespace Fig.Web.Services;

public interface IAccountService
{
    AuthenticatedUserModel? AuthenticatedUser { get; }
    
    bool IsInitialized { get; }

    Task Initialize();

    Task Login(LoginModel model);

    Task Logout();

    Task<Guid> Register(RegisterUserRequestDataContract model);

    Task<IList<UserDataContract>> GetAll();

    Task Update(Guid id, UpdateUserRequestDataContract model);

    Task Delete(Guid id);
}