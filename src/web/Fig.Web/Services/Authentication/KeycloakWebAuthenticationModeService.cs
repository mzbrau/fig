using System.Security.Claims;
using Fig.Client.Abstractions.Data;
using Fig.Common.Events;
using Fig.Contracts.Authentication;
using Fig.Web.Events;
using Fig.Web.Models.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Authentication;
using Newtonsoft.Json.Linq;

namespace Fig.Web.Services.Authentication;

public class KeycloakWebAuthenticationModeService : IWebAuthenticationModeService
{
    private readonly IAccessTokenProvider _accessTokenProvider;
    private readonly AuthenticationStateProvider _authenticationStateProvider;
    private readonly IEventDistributor _eventDistributor;
    private readonly ILocalStorageService _localStorageService;
    private readonly NavigationManager _navigationManager;

    public KeycloakWebAuthenticationModeService(
        NavigationManager navigationManager,
        ILocalStorageService localStorageService,
        IEventDistributor eventDistributor,
        IAccessTokenProvider accessTokenProvider,
        AuthenticationStateProvider authenticationStateProvider)
    {
        _navigationManager = navigationManager;
        _localStorageService = localStorageService;
        _eventDistributor = eventDistributor;
        _accessTokenProvider = accessTokenProvider;
        _authenticationStateProvider = authenticationStateProvider;

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

        var role = ResolveRole(principal);
        var allowedClassifications = ResolveAllowedClassifications(principal, role);
        var username = ClaimValue(principal, "preferred_username")
                       ?? ClaimValue(principal, ClaimTypes.Name)
                       ?? ClaimValue(principal, "sub")
                       ?? "unknown";

        AuthenticatedUser = new AuthenticatedUserModel
        {
            Id = null,
            Username = username,
            FirstName = ClaimValue(principal, "given_name") ?? username,
            LastName = ClaimValue(principal, "family_name"),
            Role = role,
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
            }
        });
    }

    private static Role ResolveRole(ClaimsPrincipal principal)
    {
        var roleClaims = principal.Claims
            .SelectMany(ExtractRoleValues)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (roleClaims.Contains(Role.Administrator.ToString()))
            return Role.Administrator;

        if (roleClaims.Contains(Role.ReadOnly.ToString()))
            return Role.ReadOnly;

        if (roleClaims.Contains(Role.LookupService.ToString()))
            return Role.LookupService;

        return Role.User;
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
        catch
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
        catch
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
        catch
        {
            return [];
        }
    }

    private static List<Classification> ResolveAllowedClassifications(ClaimsPrincipal principal, Role role)
    {
        var claimValue = ClaimValue(principal, "fig_allowed_classifications");

        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return role == Role.Administrator
                ? Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList()
                : [];
        }

        var values = claimValue.Trim().StartsWith("[")
            ? claimValue
                .Trim('[', ']')
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(a => a.Trim('"'))
            : claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return values
            .Where(a => Enum.TryParse<Classification>(a, true, out _))
            .Select(a => Enum.Parse<Classification>(a, true))
            .ToList();
    }

    private static string? ClaimValue(ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims.FirstOrDefault(a => a.Type == claimType)?.Value;
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
