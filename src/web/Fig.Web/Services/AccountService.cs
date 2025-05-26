using Fig.Common.Events;
using Fig.Common.NetStandard.Data;
using Fig.Contracts.Authentication;
using Fig.Web.Converters;
using Fig.Common.Events;
using Fig.Common.NetStandard.Data;
using Fig.Contracts.Authentication;
using Fig.Web.Converters;
using Fig.Common.Events;
using Fig.Common.NetStandard.Data;
using Fig.Contracts.Authentication;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text;

namespace Fig.Web.Services;

public class AccountService : IAccountService
{
    private readonly IHttpService _httpService;
    private readonly ILocalStorageService _localStorageService;
    private readonly NavigationManager _navigationManager;
    private readonly IUserConverter _userConverter;
    private readonly IEventDistributor _eventDistributor;
    private readonly WebSettings _webSettings;
    private readonly SignOutSessionStateManager _signOutSessionStateManager;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly string _userKey = "user";

    public AccountService(
        IHttpService httpService,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService,
        IUserConverter userConverter,
        IEventDistributor eventDistributor,
        IOptions<WebSettings> webSettings,
        SignOutSessionStateManager signOutSessionStateManager,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _httpService = httpService;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _userConverter = userConverter;
        _eventDistributor = eventDistributor;
        _webSettings = webSettings.Value;
        _signOutSessionStateManager = signOutSessionStateManager;
        _authenticationStateProvider = authenticationStateProvider;
    }

    public AuthenticatedUserModel? AuthenticatedUser { get; private set; }

    public async Task Initialize()
    {
        if (_webSettings.UseKeycloak)
        {
            var authState = await _authenticationStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (user.Identity is { IsAuthenticated: true })
            {
                var subClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var usernameClaim = user.FindFirst(ClaimTypes.Name)?.Value ??
                                    user.FindFirst("preferred_username")?.Value;
                
                Guid userId = Guid.Empty;
                if (subClaim != null)
                {
                    using var sha256 = System.Security.Cryptography.SHA256.Create();
                    var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(subClaim));
                    var guidBytes = new byte[16];
                    Array.Copy(hash, guidBytes, 16);
                    userId = new Guid(guidBytes);
                }

                var roleClaim = user.FindAll(ClaimTypes.Role).FirstOrDefault()?.Value ??
                                user.FindFirst("client_role")?.Value ??
                                user.FindFirst("realm_role")?.Value;

                Role figRole = Role.User; // Default role
                if (roleClaim != null && Enum.TryParse<Role>(roleClaim, true, out var parsedRole))
                {
                    figRole = parsedRole;
                }
                
                AuthenticatedUser = new AuthenticatedUserModel
                {
                    Id = userId,
                    Username = usernameClaim ?? "Unknown",
                    FirstName = user.FindFirst(ClaimTypes.GivenName)?.Value,
                    LastName = user.FindFirst(ClaimTypes.Surname)?.Value,
                    Role = figRole,
                    Token = null, // Token is managed by OIDC handler
                    PasswordChangeRequired = false, // Password management is external
                    AllowedClassifications = Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList() // Default, may need adjustment
                };
            }
            else
            {
                AuthenticatedUser = null;
            }
        }
        else
        {
            AuthenticatedUser = await _localStorageService.GetItem<AuthenticatedUserModel>(_userKey);

            // Required as this could be null when the property was introduced. Can be removed in a later version.
            if (AuthenticatedUser is not null && AuthenticatedUser.AllowedClassifications is null)
            {
                AuthenticatedUser.AllowedClassifications =
                    Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList();
            }
        }
    }

    public async Task Login(LoginModel model)
    {
        if (_webSettings.UseKeycloak)
        {
            _navigationManager.NavigateTo("authentication/login");
            return;
        }
        
        var dataContract = new AuthenticateRequestDataContract(model.Username!, model.Password!);
        var user = await _httpService.Post<AuthenticateResponseDataContract>("/users/authenticate", dataContract);

        if (user == null)
            throw new Exception("Invalid user");

        AuthenticatedUser = _userConverter.Convert(user);
        await _localStorageService.SetItem(_userKey, AuthenticatedUser);
    }

    public async Task Logout()
    {
        if (_webSettings.UseKeycloak)
        {
            await _signOutSessionStateManager.SetSignOutState();
            _navigationManager.NavigateTo("authentication/logout");
            return;
        }
        
        AuthenticatedUser = null;
        await _localStorageService.RemoveItem(_userKey);
        await _eventDistributor.PublishAsync(EventConstants.LogoutEvent);
        _navigationManager.NavigateTo("account/login");
    }

    public async Task<Guid> Register(RegisterUserRequestDataContract userRegistration)
    {
        if (_webSettings.UseKeycloak)
        {
            // User registration is handled by Keycloak.
            return Guid.Empty; 
        }
        return await _httpService.Post<Guid>("/users/register", userRegistration);
    }

    public async Task<IList<UserDataContract>> GetAll()
    {
        if (_webSettings.UseKeycloak)
        {
            // User management is handled by Keycloak.
            return new List<UserDataContract>();
        }
        return await _httpService.Get<IList<UserDataContract>>("/users") ?? new List<UserDataContract>();
    }

    public async Task Update(Guid id, UpdateUserRequestDataContract update)
    {
        if (_webSettings.UseKeycloak)
        {
            // User updates are handled by Keycloak.
            return;
        }
        
        await _httpService.Put($"/users/{id}", update);

        // update stored user if the logged in user updated their own record
        if (id == AuthenticatedUser?.Id)
        {
            // update local storage
            AuthenticatedUser.FirstName = update.FirstName;
            AuthenticatedUser.LastName = update.LastName;
            AuthenticatedUser.Username = update.Username;
            await _localStorageService.SetItem(_userKey, AuthenticatedUser);
        }

        if (AuthenticatedUser?.PasswordChangeRequired == true)
        {
            await Logout();
        }
    }

    public async Task Delete(Guid id)
    {
        if (_webSettings.UseKeycloak)
        {
            // User deletion is handled by Keycloak.
            return;
        }
        
        await _httpService.Delete($"/users/{id}");

        // auto logout if the logged in user deleted their own record
        if (id == AuthenticatedUser?.Id)
            await Logout();
    }
}