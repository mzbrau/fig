using Fig.Web.Models.Authentication;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fig.Web.Services.Authentication;

public sealed class FigApiAccessTokenProvider : IFigApiAccessTokenProvider
{
    private readonly WebAuthMode _authenticationMode;
    private readonly ILocalStorageService _localStorageService;
    private readonly IServiceProvider _serviceProvider;

    public FigApiAccessTokenProvider(
        IOptions<WebSettings> webSettings,
        ILocalStorageService localStorageService,
        IServiceProvider serviceProvider)
    {
        _authenticationMode = webSettings.Value.Authentication.Mode;
        _localStorageService = localStorageService;
        _serviceProvider = serviceProvider;
    }

    public async Task<string?> GetAccessTokenAsync()
    {
        if (_authenticationMode == WebAuthMode.Keycloak)
        {
            var accessTokenProvider = _serviceProvider.GetService<IAccessTokenProvider>();
            if (accessTokenProvider is null)
                return null;

            var tokenResult = await accessTokenProvider.RequestAccessToken();
            return tokenResult.TryGetToken(out var token) ? token.Value : null;
        }

        var user = await _localStorageService.GetItem<AuthenticatedUserModel>(
            WebAuthenticationConstants.AuthenticatedUserStorageKey);
        return string.IsNullOrWhiteSpace(user?.Token) ? null : user.Token;
    }
}
