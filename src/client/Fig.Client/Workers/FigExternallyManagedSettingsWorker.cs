using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Status;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Status;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Fig.Client.Workers;

public class FigExternallyManagedSettingsWorker<T> : IHostedService where T : SettingsBase
{
    private readonly ILogger<FigExternallyManagedSettingsWorker<T>> _logger;
    private readonly IConfiguration _configuration;
    private readonly T _settings;
    private bool _hasDetected;

    public FigExternallyManagedSettingsWorker(
        ILogger<FigExternallyManagedSettingsWorker<T>> logger,
        IConfiguration configuration,
        T settings)
    {
        _logger = logger;
        _configuration = configuration;
        _settings = settings;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        // Delay slightly to ensure Fig configuration has been loaded
        await Task.Delay(TimeSpan.FromMilliseconds(500), cancellationToken);
        
        if (_hasDetected)
            return;
            
        _hasDetected = true;
        DetectExternallyManagedSettings();
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void DetectExternallyManagedSettings()
    {
        try
        {
            var externallyManagedSettings = new List<ExternallyManagedSettingDataContract>();
            var figProviderData = GetFigProviderData();
            
            if (figProviderData == null)
            {
                _logger.LogDebug("Fig configuration provider not found, skipping externally managed settings detection");
                return;
            }

            var settingProperties = GetSettingProperties(_settings).ToList();
            
            foreach (var settingProperty in settingProperties)
            {
                var settingName = settingProperty.Name;
                var actualValue = settingProperty.GetValue(_settings);
                
                if (!figProviderData.TryGetValue(settingName, out var figValue))
                    continue;

                var actualValueJson = JsonConvert.SerializeObject(actualValue, JsonSettings.FigDefault);
                
                // Check if the actual value differs from the Fig-provided value
                if (!AreJsonEquivalent(figValue, actualValueJson))
                {
                    _logger.LogInformation(
                        "Setting '{SettingName}' is externally managed - Fig value differs from actual value",
                        settingName);
                    
                    externallyManagedSettings.Add(new ExternallyManagedSettingDataContract(settingName, actualValue));
                }
            }

            if (externallyManagedSettings.Count > 0)
            {
                _logger.LogInformation(
                    "Detected {Count} externally managed setting(s): {SettingNames}",
                    externallyManagedSettings.Count,
                    string.Join(", ", externallyManagedSettings.Select(s => s.Name)));
                
                ExternallyManagedSettingsBridge.ExternallyManagedSettings = externallyManagedSettings;
            }
            else
            {
                _logger.LogDebug("No externally managed settings detected");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error detecting externally managed settings");
        }
    }

    private Dictionary<string, string?>? GetFigProviderData()
    {
        // Try to find the Fig configuration provider and get its data
        if (_configuration is IConfigurationRoot configRoot)
        {
            var figProvider = configRoot.Providers
                .OfType<FigConfigurationProvider>()
                .FirstOrDefault();

            if (figProvider != null)
            {
                var result = new Dictionary<string, string?>();
                // Get all keys using public GetChildKeys method recursively
                var allKeys = GetAllKeysRecursive(figProvider, null, []);
                foreach (var key in allKeys)
                {
                    if (figProvider.TryGet(key, out var value))
                    {
                        result[key] = value;
                    }
                }
                return result;
            }
        }

        return null;
    }

    private static IEnumerable<string> GetAllKeysRecursive(
        Microsoft.Extensions.Configuration.IConfigurationProvider provider, 
        string? parentPath,
        HashSet<string> visited)
    {
        var childKeys = provider.GetChildKeys([], parentPath);
        
        foreach (var key in childKeys)
        {
            var fullKey = string.IsNullOrEmpty(parentPath) ? key : $"{parentPath}:{key}";
            
            if (visited.Contains(fullKey))
                continue;
                
            visited.Add(fullKey);
            yield return fullKey;

            // Recursively get nested keys
            foreach (var nestedKey in GetAllKeysRecursive(provider, fullKey, visited))
            {
                yield return nestedKey;
            }
        }
    }

    private static IEnumerable<PropertyInfo> GetSettingProperties(object instance, string prefix = "")
    {
        var properties = instance.GetType().GetProperties();
        
        foreach (var prop in properties)
        {
            if (Attribute.IsDefined(prop, typeof(SettingAttribute)))
            {
                yield return prop;
            }
            else if (Attribute.IsDefined(prop, typeof(NestedSettingAttribute)))
            {
                var nestedInstance = prop.GetValue(instance);
                if (nestedInstance != null)
                {
                    foreach (var nestedProp in GetSettingProperties(nestedInstance))
                    {
                        yield return nestedProp;
                    }
                }
            }
        }
    }

    private bool AreJsonEquivalent(string? value1, string? value2)
    {
        // Handle null/empty cases
        var v1Empty = string.IsNullOrEmpty(value1);
        var v2Empty = string.IsNullOrEmpty(value2);
        
        if (v1Empty && v2Empty)
            return true;

        if (v1Empty || v2Empty)
            return false;

        // At this point both values are non-null and non-empty
        // Handle simple value comparison (non-JSON strings)
        if (!value1.StartsWith("{", StringComparison.Ordinal) && 
            !value1.StartsWith("[", StringComparison.Ordinal))
        {
            // Compare as simple strings, accounting for JSON string quotes
            var normalizedValue2 = value2.Trim('"');
            return string.Equals(value1, normalizedValue2, StringComparison.Ordinal);
        }

        // For complex objects, compare JSON
        return string.Equals(value1, value2, StringComparison.Ordinal);
    }
}
