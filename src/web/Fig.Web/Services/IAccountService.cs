using Fig.Contracts.Authentication;
using Fig.Web.Models.Authentication;

namespace Fig.Web.Services;

public interface IAccountService
{
    AuthenticatedUserModel AuthenticatedUser { get; }

    Task Initialize();

    Task Login(AuthenticateRequestDataContract model);

    Task Logout();

    Task<Guid> Register(RegisterUserRequestDataContract model);

    Task<IList<UserDataContract>> GetAll();

    Task<UserDataContract> GetById(Guid id);

    Task Update(Guid id, UpdateUserRequestDataContract model);

    Task Delete(Guid id);
}