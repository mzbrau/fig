using System.IdentityModel.Tokens.Jwt;
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
using Newtonsoft.Json.Linq;

namespace Fig.Api.Authorization.UserAuth;

public class KeycloakUserAuthenticationModeService : IUserAuthenticationModeService
{
    private readonly IConfigurationManager<OpenIdConnectConfiguration> _configurationManager;
    private readonly ApiSettings _apiSettings;
    private readonly ILogger<KeycloakUserAuthenticationModeService> _logger;

    public KeycloakUserAuthenticationModeService(
        IOptions<ApiSettings> apiSettings,
        ILogger<KeycloakUserAuthenticationModeService> logger)
    {
        _apiSettings = apiSettings.Value;
        _logger = logger;

        var keycloakSettings = _apiSettings.Authentication.Keycloak;
        var authority = keycloakSettings.Authority?.TrimEnd('/');
        var metadataAddress = $"{authority}/.well-known/openid-configuration";

        _configurationManager = new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress,
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetriever { RequireHttps = keycloakSettings.RequireHttpsMetadata });
    }

    public ApiAuthMode Mode => ApiAuthMode.Keycloak;

    public async Task<UserDataContract?> ResolveAuthenticatedUser(HttpContext context)
    {
        var token = ExtractBearerToken(context);
        if (string.IsNullOrWhiteSpace(token))
            return null;

        var keycloakSettings = _apiSettings.Authentication.Keycloak;
        var config = await _configurationManager.GetConfigurationAsync(context.RequestAborted);
        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuers =
            [
                config.Issuer,
                keycloakSettings.Authority?.TrimEnd('/')
            ],
            ValidateAudience = !string.IsNullOrWhiteSpace(keycloakSettings.Audience),
            ValidAudience = keycloakSettings.Audience,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            IssuerSigningKeys = config.SigningKeys,
            ClockSkew = TimeSpan.FromMinutes(1)
        };

        var tokenHandler = new JwtSecurityTokenHandler();
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

        var jwt2 = tokenHandler.ReadJwtToken(token);

        var role = ResolveRole(principal, jwt, keycloakSettings);
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

    private static string? ExtractBearerToken(HttpContext context)
    {
        var authHeader = context.Request.Headers["Authorization"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(authHeader))
            return null;

        if (!authHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
            return null;

        return authHeader["Bearer ".Length..].Trim();
    }

    private static Role? ResolveRole(
        ClaimsPrincipal principal,
        JwtSecurityToken token,
        KeycloakAuthenticationSettings settings)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in principal.Claims.Where(c => c.Type is ClaimTypes.Role or "role" or "roles"))
            roles.Add(claim.Value);

        foreach (var role in GetValuesAtPath(token, settings.RoleClaimPath))
            roles.Add(role);

        if (!string.IsNullOrWhiteSpace(settings.AdditionalRoleClaimPath))
        {
            foreach (var role in GetValuesAtPath(token, settings.AdditionalRoleClaimPath))
                roles.Add(role);
        }

        if (roles.Contains(settings.AdminRoleName))
            return Role.Administrator;

        if (roles.Contains(Role.ReadOnly.ToString()))
            return Role.ReadOnly;

        if (roles.Contains(Role.LookupService.ToString()))
            return Role.LookupService;

        if (roles.Contains(Role.User.ToString()))
            return Role.User;

        return null;
    }

    private static List<Classification>? ResolveAllowedClassifications(
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

        var jToken = JObject.Parse(token.Payload.SerializeToJson()).SelectToken(path);
        if (jToken == null)
            return [];

        return jToken.Type == JTokenType.Array
            ? jToken.Values<string>().Where(a => !string.IsNullOrWhiteSpace(a))
            : [jToken.ToString()];
    }

    private static string? GetStringClaim(ClaimsPrincipal principal, string claimType)
    {
        return principal.Claims.FirstOrDefault(a => a.Type == claimType)?.Value;
    }

    private static bool IsValidRegex(string regexPattern)
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
}
