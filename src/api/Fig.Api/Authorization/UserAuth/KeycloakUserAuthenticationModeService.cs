using System.IdentityModel.Tokens.Jwt;
using System.Collections.Concurrent;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Fig.Api.Authorization.UserAuth;

public class KeycloakUserAuthenticationModeService : IUserAuthenticationModeService
{
    private readonly ConcurrentDictionary<string, IConfigurationManager<OpenIdConnectConfiguration>> _configurationManagers = new();
    private readonly IOptionsMonitor<ApiSettings> _apiSettings;
    private readonly ILogger<KeycloakUserAuthenticationModeService> _logger;

    public KeycloakUserAuthenticationModeService(
        IOptionsMonitor<ApiSettings> apiSettings,
        ILogger<KeycloakUserAuthenticationModeService> logger)
    {
        _apiSettings = apiSettings;
        _logger = logger;
    }

    public ApiAuthMode Mode => ApiAuthMode.Keycloak;

    public async Task<UserDataContract?> ResolveAuthenticatedUser(HttpContext context)
    {
        var token = ExtractBearerToken(context);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var keycloakSettings = _apiSettings.CurrentValue.Authentication.Keycloak;
        var configurationManager = GetConfigurationManager(keycloakSettings);
        var config = await configurationManager.GetConfigurationAsync(context.RequestAborted);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers =
            [
                config.Issuer,
                keycloakSettings.Authority?.TrimEnd('/')
            ],
            ValidateAudience = true,
            ValidAudience = keycloakSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var tokenHandler = new JwtSecurityTokenHandler { MapInboundClaims = false };
        ClaimsPrincipal principal;
        JwtSecurityToken jwt;

        try
        {
            principal = tokenHandler.ValidateToken(token, validationParameters, out var validatedToken);
            jwt = (JwtSecurityToken)validatedToken;
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning(ex, "Keycloak token validation failed");
            return null;
        }

        var role = ResolveRole(jwt, keycloakSettings);
        if (role == null)
        {
            _logger.LogWarning("No recognized Fig role found in Keycloak token claims");
            return null;
        }

        var classifications = ResolveAllowedClassifications(principal, role.Value, keycloakSettings);
        if (classifications == null)
        {
            _logger.LogWarning("No valid allowed classifications found for non-admin user in Keycloak token");
            return null;
        }

        var username = GetStringClaim(principal, keycloakSettings.UsernameClaim)
                       ?? GetStringClaim(principal, ClaimTypes.Name)
                       ?? GetStringClaim(principal, "sub");

        if (string.IsNullOrWhiteSpace(username))
        {
            _logger.LogWarning("No username claim found in Keycloak token");
            return null;
        }

        var firstName = GetStringClaim(principal, keycloakSettings.FirstNameClaim)
                        ?? GetStringClaim(principal, keycloakSettings.NameClaim)
                        ?? username;
        var lastName = GetStringClaim(principal, keycloakSettings.LastNameClaim) ?? string.Empty;
        var clientFilter = GetStringClaim(principal, keycloakSettings.ClientFilterClaim) ?? ".*";
        if (!IsValidRegex(clientFilter))
        {
            _logger.LogWarning("Invalid client filter regex '{ClientFilter}' in Keycloak token for user {Username}",
                clientFilter, username);
            return null;
        }

        var subject = GetStringClaim(principal, "sub") ?? username;

        return new UserDataContract(
            CreateDeterministicGuid(subject),
            username,
            firstName,
            lastName,
            role.Value,
            clientFilter,
            classifications);
    }

    internal static string? ExtractBearerToken(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeader["Bearer ".Length..].Trim();
    }

    /// <summary>
    /// Resolves the Fig role exclusively from configured claim paths so RoleClaimPaths remain an authorization boundary.
    /// </summary>
    internal static Role? ResolveRole(
        JwtSecurityToken token,
        KeycloakAuthenticationSettings settings)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claimPath in GetConfiguredRoleClaimPaths(settings))
        {
            foreach (var role in GetValuesAtPath(token, claimPath))
                roles.Add(role);
        }

        if (ContainsMappedRole(roles, Role.Administrator, settings, settings.AdminRoleName))
            return Role.Administrator;

        if (ContainsMappedRole(roles, Role.ReadOnly, settings))
            return Role.ReadOnly;

        if (ContainsMappedRole(roles, Role.LookupService, settings))
            return Role.LookupService;

        if (ContainsMappedRole(roles, Role.Dashboard, settings))
            return Role.Dashboard;

        if (ContainsMappedRole(roles, Role.User, settings))
            return Role.User;

        return null;
    }

    internal static List<Classification>? ResolveAllowedClassifications(
        ClaimsPrincipal principal,
        Role role,
        KeycloakAuthenticationSettings settings)
    {
        var claimValue = GetStringClaim(principal, settings.AllowedClassificationsClaim);
        if (string.IsNullOrWhiteSpace(claimValue))
        {
            return role == Role.Administrator
                ? Enum.GetValues(typeof(Classification)).Cast<Classification>().ToList()
                : null;
        }

        var classifications = new List<Classification>();
        IEnumerable<string?> values;

        try
        {
            values = claimValue.Trim().StartsWith("[")
                ? JArray.Parse(claimValue).Values<string>()
                : claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
        catch (Newtonsoft.Json.JsonReaderException)
        {
            return null;
        }

        foreach (var value in values)
        {
            if (value != null && Enum.TryParse<Classification>(value, true, out var classification))
                classifications.Add(classification);
        }

        if (classifications.Count == 0)
            return null;

        return classifications;
    }

    private static IEnumerable<string> GetValuesAtPath(JwtSecurityToken token, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return [];

        try
        {
            var jToken = JObject.Parse(token.Payload.SerializeToJson()).SelectToken(path);
            if (jToken == null)
                return [];

            return jToken.Type == JTokenType.Array
                ? jToken.Values<string?>().Where(a => !string.IsNullOrWhiteSpace(a)).Select(a => a!)
                : [jToken.ToString()];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? GetStringClaim(ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims.FirstOrDefault(a => a.Type == claimType)?.Value;
    }

    internal static bool IsValidRegex(string regexPattern)
    {
        try
        {
            _ = Regex.IsMatch(string.Empty, regexPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
        {
            return false;
        }
    }

    private static Guid CreateDeterministicGuid(string source)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes[..16].CopyTo(guidBytes);
        return new Guid(guidBytes);
    }

    private IConfigurationManager<OpenIdConnectConfiguration> GetConfigurationManager(
        KeycloakAuthenticationSettings keycloakSettings)
    {
        var authority = keycloakSettings.Authority?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(authority))
            throw new InvalidOperationException("Keycloak authority is not configured");

        var cacheKey = $"{authority}|{keycloakSettings.RequireHttpsMetadata}";
        return _configurationManagers.GetOrAdd(cacheKey, _ =>
        {
            var metadataAddress = $"{authority}/.well-known/openid-configuration";
            return new ConfigurationManager<OpenIdConnectConfiguration>(
                metadataAddress,
                new OpenIdConnectConfigurationRetriever(),
                new HttpDocumentRetriever { RequireHttps = keycloakSettings.RequireHttpsMetadata });
        });
    }

    private static IEnumerable<string> GetConfiguredRoleClaimPaths(KeycloakAuthenticationSettings settings)
    {
        foreach (var path in settings.RoleClaimPaths.Where(a => !string.IsNullOrWhiteSpace(a)))
            yield return path;

        if (!string.IsNullOrWhiteSpace(settings.RoleClaimPath))
            yield return settings.RoleClaimPath;

        if (!string.IsNullOrWhiteSpace(settings.AdditionalRoleClaimPath))
            yield return settings.AdditionalRoleClaimPath;
    }

    private static bool ContainsMappedRole(
        HashSet<string> tokenValues,
        Role role,
        KeycloakAuthenticationSettings settings,
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
}
