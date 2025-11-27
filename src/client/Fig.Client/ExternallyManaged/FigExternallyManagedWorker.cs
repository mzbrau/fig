using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Contracts.Status;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fig.Client.ExternallyManaged;

public class FigExternallyManagedWorker<T> : IHostedService where T : SettingsBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<FigExternallyManagedWorker<T>> _logger;
    private readonly List<string> _excludedPrefixes = ["LastFigUpdateUtcTicks", "FigSettingLoadType", "RestartRequested"];
    private bool _hasChecked;

    public FigExternallyManagedWorker(
        IConfiguration configuration,
        ILogger<FigExternallyManagedWorker<T>> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (_hasChecked)
            return Task.CompletedTask;

        _hasChecked = true;

        try
        {
            CheckForExternallyManagedSettings();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking for externally managed settings");
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void CheckForExternallyManagedSettings()
    {
        var figValues = FigValuesStore.GetFigValues();
        if (!figValues.Any())
        {
            _logger.LogDebug("No Fig values stored, skipping externally managed settings check");
            return;
        }

        var externallyManagedSettings = new List<ExternallyManagedSettingDataContract>();

        foreach (var figValue in figValues)
        {
            // Skip metadata properties
            if (_excludedPrefixes.Any(prefix => figValue.Key.StartsWith(prefix)))
                continue;

            var actualValue = _configuration[figValue.Key];

            // If the actual value differs from the Fig value, the setting is externally managed
            if (!string.Equals(figValue.Value, actualValue, StringComparison.Ordinal))
            {
                _logger.LogInformation(
                    "Setting '{SettingName}' is externally managed. Fig value differs from actual configuration value",
                    figValue.Key);
                
                externallyManagedSettings.Add(new ExternallyManagedSettingDataContract(
                    figValue.Key,
                    actualValue));
            }
        }

        if (externallyManagedSettings.Any())
        {
            _logger.LogInformation(
                "Detected {Count} externally managed settings: {SettingNames}",
                externallyManagedSettings.Count,
                string.Join(", ", externallyManagedSettings.Select(s => s.Name)));
            
            ExternallyManagedSettingsBridge.SetExternallyManagedSettings(externallyManagedSettings);
        }
        else
        {
            _logger.LogDebug("No externally managed settings detected");
        }
    }
}
