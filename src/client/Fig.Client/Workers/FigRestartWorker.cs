using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fig.Client.Workers;

public class FigRestartWorker<T> : IHostedService, IDisposable where T : SettingsBase
{
    private readonly IOptionsMonitor<T> _settings;
    private readonly ILogger<FigRestartWorker<T>> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;
    private IDisposable? _restartRegistration;
    private bool _disposed;

    public FigRestartWorker(IOptionsMonitor<T> settings, ILogger<FigRestartWorker<T>> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        _settings = settings;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
        RestartStore.SupportsRestart = true;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        DisposeRegistration();
        _disposed = false;
        _restartRegistration = _settings.OnChange(OnSettingsChanged);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        Dispose();
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        DisposeRegistration();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private void OnSettingsChanged(T settings, string? name)
    {
        if (settings.RestartRequested)
        {
            _logger.LogInformation("Exiting application due to restart request from Fig.");
            _hostApplicationLifetime.StopApplication();
        }
    }

    private void DisposeRegistration()
    {
        _restartRegistration?.Dispose();
        _restartRegistration = null;
    }
}
