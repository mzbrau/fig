using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fig.Web.Services.Authentication;

public class WebAuthenticationModeService : IWebAuthenticationModeService
{
    private readonly AuthenticationModeSelector _authenticationModeSelector;

    public WebAuthenticationModeService(IServiceProvider serviceProvider, IOptions<WebSettings> webSettings)
    {
        _authenticationModeSelector = new AuthenticationModeSelector(serviceProvider, webSettings.Value);
    }

    public WebAuthMode Mode => _authenticationModeSelector.Mode;

    public Fig.Web.Models.Authentication.AuthenticatedUserModel? AuthenticatedUser => _authenticationModeSelector.Current.AuthenticatedUser;

    public bool IsInitialized => _authenticationModeSelector.Current.IsInitialized;

    public Task Initialize() => _authenticationModeSelector.Current.Initialize();

    public Task Login(Fig.Web.Models.Authentication.LoginModel model) => _authenticationModeSelector.Current.Login(model);

    public Task Logout() => _authenticationModeSelector.Current.Logout();

    public Task<Guid> Register(Fig.Contracts.Authentication.RegisterUserRequestDataContract model) => _authenticationModeSelector.Current.Register(model);

    public Task<IList<Fig.Contracts.Authentication.UserDataContract>> GetAll() => _authenticationModeSelector.Current.GetAll();

    public Task Update(Guid id, Fig.Contracts.Authentication.UpdateUserRequestDataContract model) => _authenticationModeSelector.Current.Update(id, model);

    public Task Delete(Guid id) => _authenticationModeSelector.Current.Delete(id);

    private class AuthenticationModeSelector
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WebSettings _settings;

        public AuthenticationModeSelector(IServiceProvider serviceProvider, WebSettings settings)
        {
            _serviceProvider = serviceProvider;
            _settings = settings;
        }

        public WebAuthMode Mode => _settings.Authentication.Mode;

        public IWebAuthenticationModeService Current => Mode switch
        {
            WebAuthMode.Keycloak => _serviceProvider.GetRequiredService<KeycloakWebAuthenticationModeService>(),
            _ => _serviceProvider.GetRequiredService<FigManagedWebAuthenticationModeService>()
        };
    }
}
