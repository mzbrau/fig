using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Health;
using Fig.Contracts.Health;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Workers;

public class FigHealthWorker<T> : IHostedService where T : SettingsBase
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);
    
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<FigHealthWorker<T>> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public FigHealthWorker(HealthCheckService healthCheckService, ILogger<FigHealthWorker<T>> logger,
        IHostApplicationLifetime hostApplicationLifetime)
    {
        _healthCheckService = healthCheckService;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        var appStopping = _hostApplicationLifetime.ApplicationStopping;
        
        HealthCheckBridge.GetHealthReportAsync = async () =>
        {
            using var timeoutCts = new CancellationTokenSource(HealthCheckTimeout);
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(appStopping, timeoutCts.Token);
            
            try
            {
                var result = await _healthCheckService.CheckHealthAsync(linkedCts.Token);
                return FigHealthReportConverter.FromHealthReport(result);
            }
            catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested)
            {
                _logger.LogWarning("Health check timed out after {Timeout} seconds and was cancelled", 
                    HealthCheckTimeout.TotalSeconds);
                
                return new HealthDataContract
                {
                    Status = FigHealthStatus.Unknown,
                    Components = new List<ComponentHealthDataContract>
                    {
                        new("HealthCheck", FigHealthStatus.Unknown, 
                            $"Health check timed out after {HealthCheckTimeout.TotalSeconds} seconds")
                    }
                };
            }
        };
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}