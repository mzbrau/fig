using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.LookupTable;
using Fig.Client.LookupTable;
using Fig.Contracts.LookupTable;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Workers;

public class FigLookupWorker<T> : IHostedService where T : SettingsBase
{
    private readonly ILogger<FigLookupWorker<T>> _logger;
    private readonly IEnumerable<ILookupProvider> _lookupProviders;
    private readonly IEnumerable<IKeyedLookupProvider> _keyedLookupProviders;
    private readonly HashSet<string> _validLookupTableNames;

    public FigLookupWorker(ILogger<FigLookupWorker<T>> logger, 
        IEnumerable<ILookupProvider> lookupProviders,
        IEnumerable<IKeyedLookupProvider> keyedLookupProviders)
    {
        _logger = logger;
        _lookupProviders = lookupProviders;
        _keyedLookupProviders = keyedLookupProviders;
        _validLookupTableNames = GetValidLookupTableNames();
    }
    
    
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        foreach (var provider in _lookupProviders)
        {
            // Only register lookup tables that have matching settings with [LookupTable] attributes
            if (_validLookupTableNames.Contains(provider.LookupName))
            {
                var items = await provider.GetItems();
                await RegisterLookupTable(provider.LookupName, items);
            }
        }

        foreach (var provider in _keyedLookupProviders)
        {
            // Only register lookup tables that have matching settings with [LookupTable] attributes
            if (_validLookupTableNames.Contains(provider.LookupName))
            {
                var items = await provider.GetItems();
                var flattenedItems = new Dictionary<string, string?>();
                foreach (var kvp in items)
                {
                    // Flatten the keyed lookup into a single dictionary
                    foreach (var innerKvp in kvp.Value)
                    {
                        flattenedItems[$"[{kvp.Key}]{innerKvp.Key}"] = innerKvp.Value;
                    }
                }
                
                await RegisterLookupTable(provider.LookupName, flattenedItems);
            }
        }
    }

    private async Task RegisterLookupTable(string lookupName, Dictionary<string, string?> items)
    {
        var lookupTable = new LookupTableDataContract(null, lookupName, items, true);
        
        if (LookupTableBridge.RegisterLookupTable is not null)
            await LookupTableBridge.RegisterLookupTable(lookupTable);
        else
        {
            _logger.LogWarning("LookupTableBridge.RegisterLookupTable is not set. Cannot register lookup table: {LookupName}", lookupName);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private HashSet<string> GetValidLookupTableNames()
    {
        var lookupTableNames = new HashSet<string>();
        var settingsType = typeof(T);
        
        // Get all properties that have both [Setting] and [LookupTable] attributes
        var properties = GetAllSettingProperties(settingsType);
        
        foreach (var property in properties)
        {
            var settingAttribute = property.GetCustomAttribute<SettingAttribute>();
            var lookupTableAttribute = property.GetCustomAttribute<LookupTableAttribute>();
            
            if (settingAttribute != null && lookupTableAttribute != null)
            {
                lookupTableNames.Add(lookupTableAttribute.LookupTableKey);
            }
        }
        
        return lookupTableNames;
    }

    private IEnumerable<PropertyInfo> GetAllSettingProperties(Type type, string prefix = "")
    {
        var properties = type.GetProperties();
        var result = new List<PropertyInfo>();

        foreach (var property in properties)
        {
            if (property.GetCustomAttribute<SettingAttribute>() != null)
            {
                result.Add(property);
            }
            else if (property.GetCustomAttribute<NestedSettingAttribute>() != null)
            {
                // Recursively get properties from nested settings
                var nestedProperties = GetAllSettingProperties(property.PropertyType, 
                    string.IsNullOrEmpty(prefix) ? property.Name : $"{prefix}:{property.Name}");
                result.AddRange(nestedProperties);
            }
        }

        return result;
    }
}