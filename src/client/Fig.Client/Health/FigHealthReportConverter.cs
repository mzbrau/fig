using System.Collections.Generic;
using Fig.Contracts.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Fig.Client.Health;

public static class FigHealthReportConverter
{
    public static HealthDataContract FromHealthReport(HealthReport report)
    {
        var figReport = new HealthDataContract
        {
            Status = ConvertStatus(report.Status),
            Components = new List<ComponentHealthDataContract>()
        };

        foreach (var entry in report.Entries)
        {
            var status = ConvertStatus(entry.Value.Status);
            var message = status == FigHealthStatus.Healthy
                ? entry.Value.Description ?? "Healthy"
                : entry.Value.Description ?? entry.Value.Exception?.Message ?? "Unhealthy";
            figReport.Components.Add(new ComponentHealthDataContract(entry.Key, status, message));
        }

        return figReport;
    }

    private static FigHealthStatus ConvertStatus(HealthStatus status)
    {
        return status switch
        {
            HealthStatus.Healthy => FigHealthStatus.Healthy,
            HealthStatus.Degraded => FigHealthStatus.Degraded,
            HealthStatus.Unhealthy => FigHealthStatus.Unhealthy,
            _ => FigHealthStatus.Unknown
        };
    }
}
