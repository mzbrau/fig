using Fig.Contracts.Authentication;

namespace Fig.Api.Authorization.UserAuth;

public interface IUserAuthenticationModeService
{
    ApiAuthMode Mode { get; }

    Task<UserDataContract?> ResolveAuthenticatedUser(HttpContext context);
}
