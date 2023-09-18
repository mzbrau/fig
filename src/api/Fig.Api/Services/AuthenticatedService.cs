using Fig.Api.ExtensionMethods;
using Fig.Contracts.Authentication;

namespace Fig.Api.Services;

public abstract class AuthenticatedService
{
    protected UserDataContract? AuthenticatedUser;
    
    public void SetAuthenticatedUser(UserDataContract user)
    {
        AuthenticatedUser = user;
    }
    
    protected void ThrowIfNoAccess(string clientName)
    {
        if (AuthenticatedUser?.HasAccess(clientName) != true)
            throw new UnauthorizedAccessException(
                $"User {AuthenticatedUser?.Username} does not have access to client {clientName}");
    }
}