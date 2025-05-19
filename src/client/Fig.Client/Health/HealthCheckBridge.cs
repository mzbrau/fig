using System;
using System.Threading.Tasks;
using Fig.Contracts.Health;

namespace Fig.Client.Health;

public static class HealthCheckBridge
{
    public static Func<Task<HealthDataContract>>? GetHealthReportAsync;
}