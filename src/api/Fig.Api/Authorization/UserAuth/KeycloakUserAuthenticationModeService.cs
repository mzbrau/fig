using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
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

    public KeycloakUserAuthenticationModeService(IOptions<ApiSettings> apiSettings)
    {
        _apiSettings = apiSettings.Value;

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
        var token = context.Request.Headers["Authorization"].FirstOrDefault()?.Split(" ").Last();
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

        try
        {
            principal = tokenHandler.ValidateToken(token, validationParameters, out _);
        }
        catch
        {
            return null;
        }

        var jwt = tokenHandler.ReadJwtToken(token);
        var role = ResolveRole(principal, jwt, keycloakSettings);
        if (role == null)
            return null;

        var classifications = ResolveAllowedClassifications(principal, role.Value, keycloakSettings);
        if (classifications == null)
            return null;

        var username = GetStringClaim(principal, keycloakSettings.UsernameClaim)
                       ?? GetStringClaim(principal, ClaimTypes.Name)
                       ?? GetStringClaim(principal, "sub");

        if (string.IsNullOrWhiteSpace(username))
            return null;

        var firstName = GetStringClaim(principal, keycloakSettings.FirstNameClaim)
                        ?? GetStringClaim(principal, keycloakSettings.NameClaim)
                        ?? username;
        var lastName = GetStringClaim(principal, keycloakSettings.LastNameClaim) ?? string.Empty;
        var clientFilter = GetStringClaim(principal, keycloakSettings.ClientFilterClaim) ?? ".*";
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

    private static Role? ResolveRole(
        ClaimsPrincipal principal,
        JwtSecurityToken token,
        KeycloakAuthenticationSettings settings)
    {
        var roles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var claim in principal.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "role" || c.Type == "roles"))
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
        var values = claimValue.Trim().StartsWith("[")
            ? JArray.Parse(claimValue).Values<string>()
            : claimValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var value in values)
        {
            if (Enum.TryParse<Classification>(value, true, out var classification))
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

    private static Guid CreateDeterministicGuid(string source)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(source));
        Span<byte> guidBytes = stackalloc byte[16];
        bytes[..16].CopyTo(guidBytes);
        return new Guid(guidBytes);
    }
}
