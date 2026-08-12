using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;

namespace Fig.Web.Services.Authentication;

public interface IWebAuthenticationModeService
{
    WebAuthMode Mode { get; }

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
