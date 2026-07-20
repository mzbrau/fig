using Fig.Api.Services;
using Fig.Api.Authorization;
using Fig.Contracts.Authentication;

namespace Fig.Api.Authorization.UserAuth;

public class FigManagedUserAuthenticationModeService : IUserAuthenticationModeService
{
    private readonly IUserService _userService;
    private readonly ITokenHandler _tokenHandler;

    public FigManagedUserAuthenticationModeService(IUserService userService, ITokenHandler tokenHandler)
    {
        _userService = userService;
        _tokenHandler = tokenHandler;
    }

    public ApiAuthMode Mode => ApiAuthMode.FigManaged;

    public async Task<UserDataContract?> ResolveAuthenticatedUser(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        string? token = null;
        if (authHeader != null)
        {
            token = authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                ? authHeader.Substring("Bearer ".Length).Trim()
                : authHeader;
        }

        var tokenData = _tokenHandler.Validate(token);
        if (tokenData == null)
            return null;

        var user = await _userService.GetById(tokenData.UserId);
        user.PasswordChangeRequired = tokenData.PasswordChangeRequired;
        return user;
    }
}
