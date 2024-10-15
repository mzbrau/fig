using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fig.Client.Workers;

public class FigRestartWorker<T> : IHostedService where T : SettingsBase
{
    private readonly IOptionsMonitor<SettingsBase> _settings;
    private readonly ILogger<FigValidationWorker<T>> _logger;
    private readonly IHostApplicationLifetime _hostApplicationLifetime;

    public FigRestartWorker(IOptionsMonitor<T> settings, ILogger<FigValidationWorker<T>> logger, IHostApplicationLifetime hostApplicationLifetime)
    {
        _settings = settings;
        _logger = logger;
        _hostApplicationLifetime = hostApplicationLifetime;
        RestartStore.SupportsRestart = true;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        _settings.OnChange((settings, _) =>
        {
            if (settings.RestartRequested)
            {
                _logger.LogInformation("Exiting application due to restart request from Fig.");
                _hostApplicationLifetime.StopApplication();
            }
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}