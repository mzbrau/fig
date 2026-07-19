namespace Fig.Api.Diagnostics;

/// <summary>
/// Diagnostic A/B switch for measuring whether setting Description CLOBs slow QueryClients.
/// Set environment variable FIG_DIAG_OMIT_SETTING_DESCRIPTIONS=1 (or true) before API start.
/// When enabled: Description is NH property-lazy and best-effort clone skips reading it
/// (empty descriptions in /clients). Compare fig.api.query_elapsed_ms / QueryClients to baseline.
/// </summary>
public static class SettingDescriptionLoadDiagnostics
{
    public const string EnvironmentVariableName = "FIG_DIAG_OMIT_SETTING_DESCRIPTIONS";

    private static readonly bool Omit =
        IsTruthy(Environment.GetEnvironmentVariable(EnvironmentVariableName));

    /// <summary>
    /// When true, setting Description is mapped lazy and omitted from best-effort /clients clones.
    /// </summary>
    public static bool OmitOnBestEffortRead => Omit;

    private static bool IsTruthy(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;

        return value is "1" or "true" or "TRUE" or "yes" or "YES";
    }
}
