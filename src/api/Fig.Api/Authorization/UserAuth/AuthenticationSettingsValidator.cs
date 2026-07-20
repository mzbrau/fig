using Fig.Contracts.Authentication;

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

        if ((keycloak.RoleClaimPaths == null || keycloak.RoleClaimPaths.All(string.IsNullOrWhiteSpace)) &&
            string.IsNullOrWhiteSpace(keycloak.RoleClaimPath) &&
            string.IsNullOrWhiteSpace(keycloak.AdditionalRoleClaimPath))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:RoleClaimPaths must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.AllowedClassificationsClaim))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:AllowedClassificationsClaim must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.ClientFilterClaim))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:ClientFilterClaim must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.Audience))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:Audience must be configured when Mode=Keycloak");

        if (string.IsNullOrWhiteSpace(keycloak.AdminRoleName))
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:AdminRoleName must be configured when Mode=Keycloak");

        if (keycloak.RoleMappings == null)
            throw new InvalidOperationException("ApiSettings:Authentication:Keycloak:RoleMappings must be configured when Mode=Keycloak");

        foreach (var roleName in Enum.GetNames<Role>())
        {
            if (!keycloak.RoleMappings.TryGetValue(roleName, out var mappedValues) ||
                mappedValues == null ||
                mappedValues.All(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException(
                    $"ApiSettings:Authentication:Keycloak:RoleMappings:{roleName} must be configured when Mode=Keycloak");
        }
    }
}
