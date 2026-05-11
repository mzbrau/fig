using Fig.Common.Events;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Newtonsoft.Json;
using System.Text;
using Radzen;

namespace Fig.Web.Services;

public class AccountService : IAccountService
{
    private readonly IHttpService _httpService;
    private readonly ILocalStorageService _localStorageService;
    private readonly NavigationManager _navigationManager;
    private readonly IUserConverter _userConverter;
    private readonly IEventDistributor _eventDistributor;
    private readonly INotificationHistoryService _notificationHistoryService;
    private readonly NotificationService _notificationService;
    private readonly string _userKey = "user";

    public AccountService(
        IHttpService httpService,
        NavigationManager navigationManager,
        ILocalStorageService localStorageService,
        IUserConverter userConverter,
        IEventDistributor eventDistributor,
        INotificationHistoryService notificationHistoryService,
        NotificationService notificationService)
    {
        _httpService = httpService;
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _userConverter = userConverter;
        _eventDistributor = eventDistributor;
        _notificationHistoryService = notificationHistoryService;
        _notificationService = notificationService;
    }

    public AuthenticatedUserModel? AuthenticatedUser { get; private set; }
    
    public bool IsInitialized { get; private set; }

    public async Task Initialize()
    {
        try
        {
            AuthenticatedUser = await _localStorageService.GetItem<AuthenticatedUserModel>(_userKey);
            
            // Required as this could be null when the property was introduced. Can be removed in a later version.
            if (AuthenticatedUser is not null && AuthenticatedUser.AllowedClassifications is null)
            {
                AuthenticatedUser.AllowedClassifications =
                    Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList();
            }
            
            // Validate the token if we have a user
            if (AuthenticatedUser != null)
            {
                var isValid = await ValidateCurrentToken();
                if (!isValid)
                {
                    await LogoutSilently();
                }
            }
        }
        finally
        {
            IsInitialized = true;
        }
    }

    private async Task<bool> ValidateCurrentToken()
    {
        if (AuthenticatedUser?.Token == null)
            return false;

        if (AuthenticatedUser.PasswordChangeRequired)
            return !IsJwtExpired(AuthenticatedUser.Token);

        try
        {
            // Make a simple authenticated request to validate the token
            // Use a lightweight endpoint that doesn't require specific permissions
            var result = await _httpService.Get<object>("/users", false); // Don't show notifications for this validation
            return result != null;
        }
        catch (HttpRequestException)
        {
            // Network/connection issue - don't invalidate token, just return true to avoid logout
            Console.WriteLine("Network issue during token validation, assuming token is valid");
            return true;
        }
        catch (Exception ex)
        {
            // Other errors (like unauthorized) should invalidate the token
            Console.WriteLine($"Token validation failed: {ex.Message}");
            return false;
        }
    }

    public async Task Login(LoginModel model)
    {
        var dataContract = new AuthenticateRequestDataContract(model.Username!, model.Password!);
        var user = await _httpService.Post<AuthenticateResponseDataContract>("/users/authenticate", dataContract);

        if (user == null)
            throw new Exception("Invalid user");

        ClearNotifications();
        AuthenticatedUser = _userConverter.Convert(user);
        await _localStorageService.SetItem(_userKey, AuthenticatedUser);
    }

    public async Task Logout()
    {
        ClearNotifications();
        AuthenticatedUser = null;
        await _localStorageService.RemoveItem(_userKey);
        await _eventDistributor.PublishAsync(EventConstants.LogoutEvent);
        
        // Only avoid navigation if we're already on the login page specifically
        var currentUri = new Uri(_navigationManager.Uri);
        if (!currentUri.AbsolutePath.Contains("/account/login", StringComparison.OrdinalIgnoreCase))
        {
            _navigationManager.NavigateTo("/account/login");
        }
    }

    private async Task LogoutSilently()
    {
        ClearNotifications();
        AuthenticatedUser = null;
        await _localStorageService.RemoveItem(_userKey);
        await _eventDistributor.PublishAsync(EventConstants.LogoutEvent);
        // Don't navigate during silent logout (used during initialization)
    }

    private static bool IsJwtExpired(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
            return true;

        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2)
                return true;

            var payload = parts[1]
                .Replace('-', '+')
                .Replace('_', '/');

            payload = payload.PadRight(payload.Length + ((4 - payload.Length % 4) % 4), '=');

            var json = Encoding.UTF8.GetString(Convert.FromBase64String(payload));
            var data = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);
            if (data == null || !data.TryGetValue("exp", out var expiryValue))
                return true;

            var expiry = Convert.ToInt64(expiryValue);
            return DateTimeOffset.UtcNow >= DateTimeOffset.FromUnixTimeSeconds(expiry);
        }
        catch
        {
            return true;
        }
    }

    public async Task<Guid> Register(RegisterUserRequestDataContract userRegistration)
    {
        return await _httpService.Post<Guid>("/users/register", userRegistration);
    }

    public async Task<IList<UserDataContract>> GetAll()
    {
        return await _httpService.Get<IList<UserDataContract>>("/users") ?? new List<UserDataContract>();
    }

    public async Task Update(Guid id, UpdateUserRequestDataContract update)
    {
        var passwordChanged = !string.IsNullOrEmpty(update.Password);
        var shouldLogoutAfterForcedPasswordChange = id == AuthenticatedUser?.Id &&
                                                   AuthenticatedUser?.PasswordChangeRequired == true &&
                                                   passwordChanged;

        await _httpService.Put($"/users/{id}", update);

        // update stored user if the logged in user updated their own record
        if (id == AuthenticatedUser?.Id)
        {
            // update local storage
            AuthenticatedUser.FirstName = update.FirstName;
            AuthenticatedUser.LastName = update.LastName;
            AuthenticatedUser.Username = update.Username;
            if (passwordChanged)
                AuthenticatedUser.PasswordChangeRequired = false;
            await _localStorageService.SetItem(_userKey, AuthenticatedUser);
        }

        if (shouldLogoutAfterForcedPasswordChange)
        {
            await Logout();
        }
    }

    public async Task Delete(Guid id)
    {
        await _httpService.Delete($"/users/{id}");

        // auto logout if the logged in user deleted their own record
        if (id == AuthenticatedUser?.Id)
            await Logout();
    }

    private void ClearNotifications()
    {
        _notificationHistoryService.Clear();
        _notificationService.Messages.Clear();
    }
}
