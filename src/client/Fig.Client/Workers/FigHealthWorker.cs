using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Health;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace Fig.Client.Workers;

public class FigHealthWorker<T> : IHostedService where T : SettingsBase
{
    private readonly HealthCheckService _healthCheckService;

    public FigHealthWorker(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        HealthCheckBridge.GetHealthReportAsync = async () =>
        {
            var result = await _healthCheckService.CheckHealthAsync(cancellationToken);
            return FigHealthReportConverter.FromHealthReport(result);
        };
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}