namespace Fig.Web;

using Fig.Contracts.Authentication;

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
    private static readonly Dictionary<string, string[]> DefaultRoleMappings = new(StringComparer.OrdinalIgnoreCase)
    {
        [Role.Administrator.ToString()] = ["Administrator", "/fig/Administrator"],
        [Role.User.ToString()] = ["User", "/fig/User"],
        [Role.ReadOnly.ToString()] = ["ReadOnly", "/fig/ReadOnly"],
        [Role.LookupService.ToString()] = ["LookupService", "/fig/LookupService"],
        [Role.Dashboard.ToString()] = ["Dashboard", "/fig/Dashboard"]
    };

    public string? Authority { get; set; }

    public string? ClientId { get; set; }

    public string Scopes { get; set; } = "openid profile email";

    public string ApiScope { get; set; } = string.Empty;

    public string ResponseType { get; set; } = "code";

    public string? PostLogoutRedirectUri { get; set; }

    public string? AccountManagementUrl { get; set; }

    /// <summary>
    /// Keycloak identity provider alias sent as OIDC <c>kc_idp_hint</c> (e.g. <c>entra-id</c>).
    /// When set and <see cref="EnableIdentityProviderHint"/> is true, Keycloak skips its login page and brokers to that IdP.
    /// </summary>
    public string? IdentityProviderHint { get; set; }

    /// <summary>
    /// When false, <see cref="IdentityProviderHint"/> is not sent even if configured.
    /// </summary>
    public bool EnableIdentityProviderHint { get; set; } = true;

    /// <summary>
    /// Optional OIDC <c>prompt</c> for normal logins (usually empty for seamless IdP SSO).
    /// </summary>
    public string? LoginPrompt { get; set; }

    /// <summary>
    /// OIDC <c>prompt</c> applied on the first login after logout so the user can pick another account.
    /// </summary>
    public string? PostLogoutLoginPrompt { get; set; } = "select_account";

    public string UsernameClaim { get; set; } = "preferred_username";

    public string FirstNameClaim { get; set; } = "given_name";

    public string LastNameClaim { get; set; } = "family_name";

    public string NameClaim { get; set; } = "name";

    public List<string> RoleClaimPaths { get; set; } = ["groups", "realm_access.roles", "resource_access.fig.roles"];

    public string AllowedClassificationsClaim { get; set; } = "fig_allowed_classifications";

    public string AdminRoleName { get; set; } = "Administrator";

    public Dictionary<string, string[]> RoleMappings { get; set; } = new(DefaultRoleMappings, StringComparer.OrdinalIgnoreCase);
}