using Fig.Client.Abstractions.Data;
using Fig.Common.Events;
using Fig.Contracts.Authentication;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Authentication;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.Services.Authentication;

public class FigManagedWebAuthenticationModeService : IWebAuthenticationModeService
{
    private readonly IHttpService _httpService;
    private readonly ILocalStorageService _localStorageService;
    private readonly NavigationManager _navigationManager;
    private readonly IUserConverter _userConverter;
    private readonly IEventDistributor _eventDistributor;

    public FigManagedWebAuthenticationModeService(
        IHttpService httpService,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService,
        IUserConverter userConverter,
        IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _userConverter = userConverter;
        _eventDistributor = eventDistributor;
    }

    public WebAuthMode Mode => WebAuthMode.FigManaged;

    public AuthenticatedUserModel? AuthenticatedUser { get; private set; }

    public bool IsInitialized { get; private set; }

    public async Task Initialize()
    {
        try
        {
            AuthenticatedUser = await _localStorageService.GetItem<AuthenticatedUserModel>(WebAuthenticationConstants.AuthenticatedUserStorageKey);

            if (AuthenticatedUser is not null && AuthenticatedUser.AllowedClassifications is null)
            {
                AuthenticatedUser.AllowedClassifications =
                    Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList();
            }

            if (AuthenticatedUser != null)
            {
                var isValid = await ValidateCurrentToken();
                if (!isValid)
                    await LogoutSilently();
            }
        }
        finally
        {
            IsInitialized = true;
        }
    }

    public async Task Login(LoginModel model)
    {
        var dataContract = new AuthenticateRequestDataContract(model.Username!, model.Password!);
        var user = await _httpService.Post<AuthenticateResponseDataContract>("/users/authenticate", dataContract);

        if (user == null)
            throw new Exception("Invalid user");

        AuthenticatedUser = _userConverter.Convert(user);
        await _localStorageService.SetItem(WebAuthenticationConstants.AuthenticatedUserStorageKey, AuthenticatedUser);
    }

    public async Task Logout()
    {
        AuthenticatedUser = null;
        await _localStorageService.RemoveItem(WebAuthenticationConstants.AuthenticatedUserStorageKey);
        await _eventDistributor.PublishAsync(EventConstants.LogoutEvent);

        var currentUri = new Uri(_navigationManager.Uri);
        if (!currentUri.AbsolutePath.Contains("/account/login", StringComparison.OrdinalIgnoreCase))
            _navigationManager.NavigateTo("/account/login");
    }

    public async Task<Guid> Register(RegisterUserRequestDataContract model)
    {
        return await _httpService.Post<Guid>("/users/register", model);
    }

    public async Task<IList<UserDataContract>> GetAll()
    {
        return await _httpService.Get<IList<UserDataContract>>("/users") ?? new List<UserDataContract>();
    }

    public async Task Update(Guid id, UpdateUserRequestDataContract model)
    {
        await _httpService.Put($"/users/{id}", model);

        if (id == AuthenticatedUser?.Id)
        {
            AuthenticatedUser.FirstName = model.FirstName;
            AuthenticatedUser.LastName = model.LastName;
            AuthenticatedUser.Username = model.Username;
            await _localStorageService.SetItem(WebAuthenticationConstants.AuthenticatedUserStorageKey, AuthenticatedUser);
        }

        if (AuthenticatedUser?.PasswordChangeRequired == true)
            await Logout();
    }

    public async Task Delete(Guid id)
    {
        await _httpService.Delete($"/users/{id}");

        if (id == AuthenticatedUser?.Id)
            await Logout();
    }

    private async Task<bool> ValidateCurrentToken()
    {
        if (AuthenticatedUser?.Token == null)
            return false;

        try
        {
            var result = await _httpService.Get<object>("/users", false);
            return result != null;
        }
        catch (HttpRequestException)
        {
            Console.WriteLine("Network issue during token validation, assuming token is valid");
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return false;
        }
    }

    private async Task LogoutSilently()
    {
        AuthenticatedUser = null;
        await _localStorageService.RemoveItem(WebAuthenticationConstants.AuthenticatedUserStorageKey);
        await _eventDistributor.PublishAsync(EventConstants.LogoutEvent);
    }
}
