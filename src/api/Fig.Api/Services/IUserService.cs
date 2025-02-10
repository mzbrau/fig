using Fig.Contracts.Authentication;

namespace Fig.Api.Services;

public interface IUserService : IAuthenticatedService
{
    Task<AuthenticateResponseDataContract> Authenticate(AuthenticateRequestDataContract model);

    Task<IEnumerable<UserDataContract>> GetAll();

    Task<UserDataContract> GetById(Guid id);

    Task<Guid> Register(RegisterUserRequestDataContract model);

    Task Update(Guid id, UpdateUserRequestDataContract model);

    Task Delete(Guid id);
}