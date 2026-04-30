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

public class FigHealthWorker<T> : IHostedService, IDisposable where T : SettingsBase
{
    private static readonly TimeSpan HealthCheckTimeout = TimeSpan.FromSeconds(5);
    
    private readonly HealthCheckService _healthCheckService;
    private readonly ILogger<FigHealthWorker<T>> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private readonly object _bridgeLock = new();
    private Func<Task<HealthDataContract>>? _healthReportProvider;

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
        
        Func<Task<HealthDataContract>> healthReportProvider = async () =>
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

        lock (_bridgeLock)
        {
            ClearHealthCheckBridge();
            _healthReportProvider = healthReportProvider;
            HealthCheckBridge.Register(healthReportProvider);
        }
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        lock (_bridgeLock)
        {
            ClearHealthCheckBridge();
        }

        return Task.CompletedTask;
    }

    public void Dispose()
    {
        lock (_bridgeLock)
        {
            ClearHealthCheckBridge();
        }
    }

    private void ClearHealthCheckBridge()
    {
        if (_healthReportProvider is null)
            return;

        HealthCheckBridge.ClearIfRegistered(_healthReportProvider);
        _healthReportProvider = null;
    }
}
