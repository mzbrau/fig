using Fig.Contracts.Authentication;

namespace Fig.Api.Services;

public interface IAuthenticatedService
{
    void SetAuthenticatedUser(UserDataContract user);
}