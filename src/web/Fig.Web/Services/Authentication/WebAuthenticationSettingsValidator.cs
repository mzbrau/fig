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
    }
}
