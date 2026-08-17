namespace Fig.Contracts.Configuration;

/// <summary>
/// Subset of Fig configuration exposed to all authenticated web users
/// so the UI can gate JavaScript-dependent features (display scripts, dashboards).
/// </summary>
public class FigWebFeaturesDataContract
{
    public bool AllowDisplayScripts { get; set; }
}
