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
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
        var userId = _tokenHandler.Validate(token);

        if (userId == null)
            return null;

        return await _userService.GetById(userId.Value);
    }
}
