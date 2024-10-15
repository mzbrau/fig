using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fig.Client.Workers;

public class FigValidationWorker<T> : IHostedService where T : SettingsBase
{
    private readonly IOptionsMonitor<SettingsBase> _settings;
    private readonly ILogger<FigValidationWorker<T>> _logger;

    public FigValidationWorker(IOptionsMonitor<T> settings, ILogger<FigValidationWorker<T>> logger)
    {
        _settings = settings;
        _logger = logger;
    }
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _settings.CurrentValue.Validate(_logger);
        
        _settings.OnChange((settings, _) =>
        {
            settings.Validate(_logger);
        });

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}