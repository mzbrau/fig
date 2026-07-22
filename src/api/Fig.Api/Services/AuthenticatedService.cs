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

    protected UserDataContract RequireAuthenticatedUser()
    {
        if (AuthenticatedUser is null)
            throw new UnauthorizedAccessException("Authenticated user is required for this operation.");

        return AuthenticatedUser;
    }

    protected void ThrowIfNoAccess(string clientName)
    {
        var user = RequireAuthenticatedUser();
        if (!user.HasAccess(clientName))
            throw new UnauthorizedAccessException(
                $"User {user.Username} does not have access to client {clientName}");
    }
}
