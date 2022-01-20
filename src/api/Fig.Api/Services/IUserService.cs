using Fig.Contracts.Authentication;

namespace Fig.Api.Services;

public interface IUserService : IAuthenticatedService
{
    AuthenticateResponseDataContract Authenticate(AuthenticateRequestDataContract model);

    IEnumerable<UserDataContract> GetAll();

    UserDataContract GetById(Guid id);

    void Register(RegisterUserRequestDataContract model);

    void Update(Guid id, UpdateUserRequestDataContract model);

    void Delete(Guid id);
}