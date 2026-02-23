using Serilog;

namespace Fig.Api.Authorization.UserAuth;

public static class AuthenticationSettingsValidator
{
    public static void Validate(ApiSettings settings)
    {
        if (settings.Authentication.Mode != AuthMode.Keycloak)
            return;

        var keycloak = settings.Authentication.Keycloak;

        if (string.IsNullOrWhiteSpace(keycloak.Authority))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:Authority must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.RoleClaimPath))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:RoleClaimPath must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.AllowedClassificationsClaim))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:AllowedClassificationsClaim must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.ClientFilterClaim))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:ClientFilterClaim must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.Audience))
            Log.Warning("ApiSettings:Authentication:Keycloak:Audience is not configured. " +
                        "Token audience validation is DISABLED. Any valid Keycloak token in the realm will be accepted");

        if (string.IsNullOrWhiteSpace(keycloak.AdminRoleName))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:AdminRoleName must be configured when Mode=Keycloak");
    }
}
