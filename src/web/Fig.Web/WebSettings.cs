namespace Fig.Web;

public class WebSettings
{
    public string ApiUri { get; set; } = "https://localhost:7281";
    
    public string? Environment { get; set; }

    public WebAuthenticationSettings Authentication { get; set; } = new();
    
    public bool DefaultDisplayCollapsed { get; set; } = true;
}

public class WebAuthenticationSettings
{
    public WebAuthMode Mode { get; set; } = WebAuthMode.FigManaged;

    public WebKeycloakAuthenticationSettings Keycloak { get; set; } = new();
}

public enum WebAuthMode
{
    FigManaged,
    Keycloak
}

public class WebKeycloakAuthenticationSettings
{
    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public string Scopes { get; set; } = "openid profile email";

    public string ApiScope { get; set; } = string.Empty;

    public string ResponseType { get; set; } = "code";

    public string? PostLogoutRedirectUri { get; set; }

    public string? AccountManagementUrl { get; set; }
}