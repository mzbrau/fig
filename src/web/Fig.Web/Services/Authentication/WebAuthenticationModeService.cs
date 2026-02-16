using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Fig.Web.Services.Authentication;

public class WebAuthenticationModeService : IWebAuthenticationModeService
{
    private readonly ApiSettingsSelector _apiSettingsSelector;

    public WebAuthenticationModeService(IServiceProvider serviceProvider, IOptions<WebSettings> webSettings)
    {
        _apiSettingsSelector = new ApiSettingsSelector(serviceProvider, webSettings.Value);
    }

    public WebAuthMode Mode => _apiSettingsSelector.Mode;

    public Fig.Web.Models.Authentication.AuthenticatedUserModel? AuthenticatedUser => _apiSettingsSelector.Current.AuthenticatedUser;

    public bool IsInitialized => _apiSettingsSelector.Current.IsInitialized;

    public Task Initialize() => _apiSettingsSelector.Current.Initialize();

    public Task Login(Fig.Web.Models.Authentication.LoginModel model) => _apiSettingsSelector.Current.Login(model);

    public Task Logout() => _apiSettingsSelector.Current.Logout();

    public Task<Guid> Register(Fig.Contracts.Authentication.RegisterUserRequestDataContract model) => _apiSettingsSelector.Current.Register(model);

    public Task<IList<Fig.Contracts.Authentication.UserDataContract>> GetAll() => _apiSettingsSelector.Current.GetAll();

    public Task Update(Guid id, Fig.Contracts.Authentication.UpdateUserRequestDataContract model) => _apiSettingsSelector.Current.Update(id, model);

    public Task Delete(Guid id) => _apiSettingsSelector.Current.Delete(id);

    private class ApiSettingsSelector
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly WebSettings _settings;

        public ApiSettingsSelector(IServiceProvider serviceProvider, WebSettings settings)
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
