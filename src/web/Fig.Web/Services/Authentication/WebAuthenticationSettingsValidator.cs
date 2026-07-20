using Fig.Contracts.Authentication;

namespace Fig.Web.Services.Authentication;

public static class WebAuthenticationSettingsValidator
{
    public static void Validate(WebSettings settings)
    {
        if (settings.Authentication.Mode != WebAuthMode.Keycloak)
            return;

        if (string.IsNullOrWhiteSpace(settings.Authentication.Keycloak.Authority))
            throw new ApplicationException("WebSettings:Authentication:Keycloak:Authority must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(settings.Authentication.Keycloak.ClientId))
            throw new ApplicationException("WebSettings:Authentication:Keycloak:ClientId must be configured when Mode=Keycloak");

        if (settings.Authentication.Keycloak.RoleClaimPaths == null ||
            settings.Authentication.Keycloak.RoleClaimPaths.All(string.IsNullOrWhiteSpace))
            throw new ApplicationException("WebSettings:Authentication:Keycloak:RoleClaimPaths must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(settings.Authentication.Keycloak.AllowedClassificationsClaim))
            throw new ApplicationException("WebSettings:Authentication:Keycloak:AllowedClassificationsClaim must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(settings.Authentication.Keycloak.AdminRoleName))
            throw new ApplicationException("WebSettings:Authentication:Keycloak:AdminRoleName must be configured when Mode=Keycloak");

        if (settings.Authentication.Keycloak.RoleMappings == null)
            throw new ApplicationException("WebSettings:Authentication:Keycloak:RoleMappings must be configured when Mode=Keycloak");

        foreach (var roleName in Enum.GetNames<Role>())
        {
            if (!settings.Authentication.Keycloak.RoleMappings.TryGetValue(roleName, out var mappedValues) ||
                mappedValues == null ||
                mappedValues.All(string.IsNullOrWhiteSpace))
                throw new ApplicationException(
                    $"WebSettings:Authentication:Keycloak:RoleMappings:{roleName} must be configured when Mode=Keycloak");
        }
    }
}
