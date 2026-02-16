namespace Fig.Api.Authorization.UserAuth;

public static class AuthenticationSettingsValidator
{
    public static void Validate(ApiSettings settings)
    {
        if (settings.Authentication.Mode != AuthMode.Keycloak)
            return;

        var keycloak = settings.Authentication.Keycloak;

        if (string.IsNullOrWhiteSpace(keycloak.Authority))
            throw new ApplicationException("ApiSettings:Authentication:Keycloak:Authority must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.RoleClaimPath))
            throw new ApplicationException("ApiSettings:Authentication:Keycloak:RoleClaimPath must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.AllowedClassificationsClaim))
            throw new ApplicationException("ApiSettings:Authentication:Keycloak:AllowedClassificationsClaim must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.ClientFilterClaim))
            throw new ApplicationException("ApiSettings:Authentication:Keycloak:ClientFilterClaim must be configured when Mode=Keycloak");
    }
}
