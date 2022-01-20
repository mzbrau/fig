using Fig.Contracts.Authentication;

namespace Fig.Api.Services;

public abstract class AuthenticatedService
{
    protected UserDataContract? AuthenticatedUser;
    
    public void SetAuthenticatedUser(UserDataContract user)
    {
        AuthenticatedUser = user;
    }
}