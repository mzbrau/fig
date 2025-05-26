namespace Fig.Web;

public class WebSettings
{
    public string ApiUri { get; set; } = "https://localhost:7281";
    
    public string? Environment { get; set; }

    public bool UseKeycloak { get; set; }

    public string? KeycloakAuthority { get; set; }

    public string? KeycloakClientId { get; set; }
}