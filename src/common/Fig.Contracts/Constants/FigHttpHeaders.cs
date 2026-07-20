namespace Fig.Contracts.Constants;

public static class FigHttpHeaders
{
    public const string ClientLoadFailures = "X-Fig-Client-Load-Failures";

    /// <summary>
    /// Comma-separated Fig.Web load-perf A/B flags (see <c>LoadPerfFlags</c>).
    /// </summary>
    public const string LoadPerf = "X-Fig-Load-Perf";
}
