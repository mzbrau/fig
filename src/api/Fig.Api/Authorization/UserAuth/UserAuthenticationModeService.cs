using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fig.Api.Authorization.UserAuth;

public class UserAuthenticationModeService : IUserAuthenticationModeService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ApiSettings _apiSettings;

    public UserAuthenticationModeService(
        IServiceProvider serviceProvider,
        IOptions<ApiSettings> apiSettings)
    {
        _serviceProvider = serviceProvider;
        _apiSettings = apiSettings.Value;
    }

    public ApiAuthMode Mode => _apiSettings.Authentication.Mode switch
    {
        AuthMode.Keycloak => ApiAuthMode.Keycloak,
        _ => ApiAuthMode.FigManaged
    };

    public Task<Fig.Contracts.Authentication.UserDataContract?> ResolveAuthenticatedUser(HttpContext context)
    {
        return Mode switch
        {
            ApiAuthMode.Keycloak => _serviceProvider.GetRequiredService<KeycloakUserAuthenticationModeService>().ResolveAuthenticatedUser(context),
            _ => _serviceProvider.GetRequiredService<FigManagedUserAuthenticationModeService>().ResolveAuthenticatedUser(context)
        };
    }
}
