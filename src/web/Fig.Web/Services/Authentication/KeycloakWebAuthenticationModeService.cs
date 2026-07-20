using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Fig.Client.Abstractions.Data;
using Fig.Common.Events;
using Fig.Contracts.Authentication;
using Fig.Web.Events;
using Fig.Web.Models.Authentication;
using Fig.Web.Notifications;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Radzen;

namespace Fig.Web.Services.Authentication;

public class KeycloakWebAuthenticationModeService : IWebAuthenticationModeService, IDisposable
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IEventDistributor _eventDistributor;
    private readonly WebKeycloakAuthenticationSettings _keycloakSettings;
    private readonly ILocalStorageService _localStorageService;
    private readonly NavigationManager _navigationManager;
    private readonly INotificationHistoryService _notificationHistoryService;
    private readonly NotificationService _notificationService;

    public KeycloakWebAuthenticationModeService(
        NavigationManager navigationManager,
        ILocalStorageService localStorageService,
        IEventDistributor eventDistributor,
        IAccessTokenProvider accessTokenProvider,
        AuthenticationStateProvider authenticationStateProvider,
        INotificationHistoryService notificationHistoryService,
        NotificationService notificationService,
        IOptions<WebSettings> webSettings)
    {
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _eventDistributor = eventDistributor;
        _accessTokenProvider = accessTokenProvider;
        _authenticationStateProvider = authenticationStateProvider;
        _notificationHistoryService = notificationHistoryService;
        _notificationService = notificationService;
        _keycloakSettings = webSettings.Value.Authentication.Keycloak;

        _authenticationStateProvider.AuthenticationStateChanged += OnAuthenticationStateChanged;
    }

    public WebAuthMode Mode => WebAuthMode.Keycloak;

    public AuthenticatedUserModel? AuthenticatedUser { get; private set; }

    public bool IsInitialized { get; private set; }

    public async Task Initialize()
    {
        try
        {
            await RefreshAuthenticatedUser();
        }
        finally
        {
            IsInitialized = true;
        }
    }

    public Task Login(LoginModel model)
    {
        var returnUrl = GetReturnUrl();
        _navigationManager.NavigateTo($"authentication/login?returnUrl={Uri.EscapeDataString(returnUrl)}", true);
        return Task.CompletedTask;
    }

    public async Task Logout()
    {
        ClearNotifications();
        AuthenticatedUser = null;
        await _localStorageService.RemoveItem(WebAuthenticationConstants.AuthenticatedUserStorageKey);
        await _eventDistributor.PublishAsync(EventConstants.LogoutEvent);

        _navigationManager.NavigateToLogout("authentication/logout");
    }

    public Task<Guid> Register(RegisterUserRequestDataContract model)
    {
        throw new NotSupportedException("User registration is not supported when Authentication.Mode is Keycloak");
    }

    public Task<IList<UserDataContract>> GetAll()
    {
        throw new NotSupportedException("User management is not supported when Authentication.Mode is Keycloak");
    }

    public Task Update(Guid id, UpdateUserRequestDataContract model)
    {
        throw new NotSupportedException("User updates are not supported when Authentication.Mode is Keycloak");
    }

    public Task Delete(Guid id)
    {
        throw new NotSupportedException("User deletion is not supported when Authentication.Mode is Keycloak");
    }

    private async Task RefreshAuthenticatedUser()
    {
        var authenticationState = await _authenticationStateProvider.GetAuthenticationStateAsync();
        await RefreshAuthenticatedUser(authenticationState.User);
    }

    private async Task RefreshAuthenticatedUser(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
        {
            AuthenticatedUser = null;
            await _localStorageService.RemoveItem(WebAuthenticationConstants.AuthenticatedUserStorageKey);
            return;
        }

        var accessTokenResult = await _accessTokenProvider.RequestAccessToken();
        if (!accessTokenResult.TryGetToken(out var token))
        {
            AuthenticatedUser = null;
            await _localStorageService.RemoveItem(WebAuthenticationConstants.AuthenticatedUserStorageKey);
            return;
        }

        var role = ResolveRole(principal, _keycloakSettings);
        if (role == null)
        {
            AuthenticatedUser = null;
            await _localStorageService.RemoveItem(WebAuthenticationConstants.AuthenticatedUserStorageKey);
            return;
        }

        var allowedClassifications = ResolveAllowedClassifications(principal, role.Value, _keycloakSettings);
        if (allowedClassifications == null)
        {
            AuthenticatedUser = null;
            await _localStorageService.RemoveItem(WebAuthenticationConstants.AuthenticatedUserStorageKey);
            return;
        }

        var username = ClaimValue(principal, _keycloakSettings.UsernameClaim)
                       ?? ClaimValue(principal, ClaimTypes.Name)
                       ?? ClaimValue(principal, "sub")
                       ?? "unknown";
        var subject = ClaimValue(principal, "sub") ?? username;

        AuthenticatedUser = new AuthenticatedUserModel
        {
            Id = CreateDeterministicGuid(subject),
            Username = username,
            FirstName = ClaimValue(principal, _keycloakSettings.FirstNameClaim)
                       ?? ClaimValue(principal, _keycloakSettings.NameClaim)
                       ?? username,
            LastName = ClaimValue(principal, _keycloakSettings.LastNameClaim),
            Role = role.Value,
            AllowedClassifications = allowedClassifications,
            Token = token.Value,
            PasswordChangeRequired = false
        };

        await _localStorageService.SetItem(WebAuthenticationConstants.AuthenticatedUserStorageKey, AuthenticatedUser);
    }

    private void OnAuthenticationStateChanged(Task<AuthenticationState> authenticationStateTask)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var authenticationState = await authenticationStateTask;
                await RefreshAuthenticatedUser(authenticationState.User);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error refreshing authentication state: {ex.Message}");
                AuthenticatedUser = null;
                await _localStorageService.RemoveItem(WebAuthenticationConstants.AuthenticatedUserStorageKey);
            }
        });
    }

    public void Dispose()
    {
        _authenticationStateProvider.AuthenticationStateChanged -= OnAuthenticationStateChanged;
    }

    private static Role? ResolveRole(ClaimsPrincipal principal, WebKeycloakAuthenticationSettings settings)
    {
        var roleClaims = principal.Claims
            .SelectMany(ExtractRoleValues)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var claimPath in settings.RoleClaimPaths.Where(a => !string.IsNullOrWhiteSpace(a)))
        {
            foreach (var role in GetValuesAtPath(principal, claimPath))
                roleClaims.Add(role);
        }

        if (ContainsMappedRole(roleClaims, Role.Administrator, settings, settings.AdminRoleName))
            return Role.Administrator;

        if (ContainsMappedRole(roleClaims, Role.ReadOnly, settings))
            return Role.ReadOnly;

        if (ContainsMappedRole(roleClaims, Role.LookupService, settings))
            return Role.LookupService;

        if (ContainsMappedRole(roleClaims, Role.User, settings))
            return Role.User;

        return null;
    }

    private static IEnumerable<string> ExtractRoleValues(Claim claim)
    {
        if (claim.Type == ClaimTypes.Role || claim.Type == "role")
            return [claim.Value];

        if (claim.Type == "roles")
        {
            var parsed = ParseStringArrayClaim(claim.Value);
            return parsed.Count > 0 ? parsed : [claim.Value];
        }

        if (claim.Type == "groups")
        {
            var parsed = ParseStringArrayClaim(claim.Value);
            return parsed.Count > 0 ? parsed : [claim.Value];
        }

        if (claim.Type == "realm_access")
            return ParseKeycloakRoleContainer(claim.Value, "roles");

        if (claim.Type == "resource_access")
            return ParseKeycloakResourceAccessRoles(claim.Value);

        return [];
    }

    private static List<string> ParseStringArrayClaim(string claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimValue) || !claimValue.TrimStart().StartsWith("["))
            return [];

        try
        {
            var token = JToken.Parse(claimValue);
            return token is JArray array
                ? array.Values<string?>().Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a!).ToList()
                : [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<string> ParseKeycloakRoleContainer(string claimValue, string propertyName)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
            return [];

        try
        {
            var token = JToken.Parse(claimValue);
            var roles = token[propertyName] as JArray;
            return roles?.Values<string?>().Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a!).ToList() ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<string> ParseKeycloakResourceAccessRoles(string claimValue)
    {
        if (string.IsNullOrWhiteSpace(claimValue))
            return [];

        try
        {
            var token = JToken.Parse(claimValue) as JObject;
            if (token == null)
                return [];

            return token.Properties()
                .SelectMany(property => (property.Value["roles"] as JArray)?.Values<string?>() ?? [])
                .Where(role => !string.IsNullOrWhiteSpace(role))
                .Select(role => role!)
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static List<Classification>? ResolveAllowedClassifications(
        ClaimsPrincipal principal,
        Role role,
        WebKeycloakAuthenticationSettings settings)
    {
        var claimValue = ClaimValue(principal, settings.AllowedClassificationsClaim);

        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return role == Role.Administrator
                ? Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList()
                : null;
        }

        IEnumerable<string?> values;
        try
        {
            values = claimValue.Trim().StartsWith("[")
                ? JArray.Parse(claimValue).Values<string>()
                : claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch (JsonException)
        {
            return null;
        }

        var classifications = values
            .Where(a => Enum.TryParse<Classification>(a, true, out _))
            .Select(a => Enum.Parse<Classification>(a, true))
            .ToList();

        return classifications.Count == 0 ? null : classifications;
    }

    private static string? ClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims.FirstOrDefault(a => a.Type == claimType)?.Value;
    }

    private static IEnumerable<string> GetValuesAtPath(ClaimsPrincipal principal, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return [];

        var splitIndex = path.IndexOf('.');
        var claimType = splitIndex > 0 ? path[..splitIndex] : path;
        var claimValue = ClaimValue(principal, claimType);
        if (string.IsNullOrWhiteSpace(claimValue))
            return [];

        if (splitIndex < 0)
        {
            var parsed = ParseStringArrayClaim(claimValue);
            return parsed.Count > 0 ? parsed : [claimValue];
        }

        try
        {
            var jToken = JToken.Parse(claimValue).SelectToken(path[(splitIndex + 1)..]);
            if (jToken == null)
                return [];

            return jToken.Type == JTokenType.Array
                ? jToken.Values<string>().Where(a => !string.IsNullOrWhiteSpace(a))
                : [jToken.ToString()];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static bool ContainsMappedRole(
        HashSet<string> tokenValues,
        Role role,
        WebKeycloakAuthenticationSettings settings,
        params string[] additionalMappedValues)
    {
        var mappedValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (settings.RoleMappings.TryGetValue(role.ToString(), out var configuredValues))
        {
            foreach (var configuredValue in configuredValues.Where(a => !string.IsNullOrWhiteSpace(a)))
                mappedValues.Add(configuredValue);
        }

        foreach (var additionalMappedValue in additionalMappedValues.Where(a => !string.IsNullOrWhiteSpace(a)))
            mappedValues.Add(additionalMappedValue);

        mappedValues.Add(role.ToString());

        return tokenValues.Any(tokenValue => MatchesAnyMappedRoleValue(tokenValue, mappedValues));
    }

    private static bool MatchesAnyMappedRoleValue(string tokenValue, HashSet<string> mappedValues)
    {
        if (mappedValues.Contains(tokenValue))
            return true;

        var groupLeaf = tokenValue.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        return !string.IsNullOrWhiteSpace(groupLeaf) &&
               mappedValues.Any(mappedValue =>
                   !mappedValue.Contains('/', StringComparison.Ordinal) &&
                   string.Equals(groupLeaf, mappedValue, StringComparison.OrdinalIgnoreCase));
    }

    private static Guid CreateDeterministicGuid(string source)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes[..16].CopyTo(guidBytes);
        return new Guid(guidBytes);
    }

    private string GetReturnUrl()
    {
        var uri = new Uri(_navigationManager.Uri);
        var query = uri.Query.TrimStart('?');
        var pairs = query.Split('&', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var pair in pairs)
        {
            var splitIndex = pair.IndexOf('=');
            if (splitIndex <= 0)
                continue;

            var key = Uri.UnescapeDataString(pair[..splitIndex]);
            if (!string.Equals(key, "returnUrl", StringComparison.OrdinalIgnoreCase))
                continue;

            var value = Uri.UnescapeDataString(pair[(splitIndex + 1)..]);
            if (!string.IsNullOrWhiteSpace(value) && IsRelativeUrl(value))
                return value;
        }

        return uri.PathAndQuery;
    }

    private void ClearNotifications()
    {
        _notificationHistoryService.Clear();
        _notificationService.Messages.Clear();
    }

    private static bool IsRelativeUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        // Must start with / and not with // (which browsers treat as protocol-relative)
        if (!url.StartsWith('/') || url.StartsWith("//"))
            return false;

        // Reject URLs with scheme indicators
        if (url.Contains("://"))
            return false;

        return true;
    }
}
