using System.Diagnostics;

namespace Fig.Api.Observability;

/// <summary>
/// Activity source for timings measured in Fig.Web and reported to the API for OTEL export.
/// Spans use this source (not Fig.Api) so Aspire clearly attributes them to the web client.
/// </summary>
public static class WebClientActivitySource
{
    public static string Name { get; } = "Fig.Web";

    public static ActivitySource Instance { get; } = new(Name, "1.0.0");
}
