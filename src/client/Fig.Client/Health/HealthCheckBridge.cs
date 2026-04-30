using System;
using System.Threading.Tasks;
using Fig.Contracts.Health;

namespace Fig.Client.Health;

public static class HealthCheckBridge
{
    private static readonly object Lock = new();
    private static Func<Task<HealthDataContract>>? _getHealthReportAsync;

    public static Func<Task<HealthDataContract>>? GetHealthReportAsync
    {
        get
        {
            lock (Lock)
            {
                return _getHealthReportAsync;
            }
        }
        set
        {
            lock (Lock)
            {
                _getHealthReportAsync = value;
            }
        }
    }

    internal static void Register(Func<Task<HealthDataContract>> getHealthReportAsync)
    {
        lock (Lock)
        {
            _getHealthReportAsync = getHealthReportAsync;
        }
    }

    internal static void ClearIfRegistered(Func<Task<HealthDataContract>> getHealthReportAsync)
    {
        lock (Lock)
        {
            if (ReferenceEquals(_getHealthReportAsync, getHealthReportAsync))
                _getHealthReportAsync = null;
        }
    }
}
