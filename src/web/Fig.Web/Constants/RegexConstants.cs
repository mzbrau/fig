namespace Fig.Web.Constants;

/// <summary>
/// Constants for regular expression configuration
/// </summary>
public static class RegexConstants
{
    /// <summary>
    /// Default timeout for regex operations to prevent catastrophic backtracking and ReDoS attacks.
    /// In Blazor WebAssembly, AppDomain.SetData("REGEX_DEFAULT_MATCH_TIMEOUT") is ignored,
    /// so this timeout must be explicitly set on each Regex instance.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(2);
}
