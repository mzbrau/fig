using Fig.Api.Services;
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
        if (string.IsNullOrWhiteSpace(authHeader) ||
            !authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        var token = authHeader["Bearer ".Length..].Trim();
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var userId = _tokenHandler.Validate(token);

        if (userId == null)
            return null;

        return await _userService.GetById(userId.Value);
    }
}
