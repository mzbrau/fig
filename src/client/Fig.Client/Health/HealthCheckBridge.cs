using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.Health;

namespace Fig.Client.Health;

public static class HealthCheckBridge
{
    public static Func<Task<HealthDataContract>>? GetHealthReportAsync;

    internal static void Register(Func<Task<HealthDataContract>> getHealthReportAsync)
    {
        Interlocked.Exchange(ref GetHealthReportAsync, getHealthReportAsync);
    }

    internal static void ClearIfRegistered(Func<Task<HealthDataContract>> getHealthReportAsync)
    {
        Interlocked.CompareExchange(ref GetHealthReportAsync, null, getHealthReportAsync);
    }
}
