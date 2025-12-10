using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.LookupTable;
using Fig.Client.Configuration;
using Fig.Client.LookupTable;
using Fig.Contracts.LookupTable;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Fig.Client.Workers;

public class FigLookupWorker<T> : IHostedService, IDisposable where T : SettingsBase
{
    private readonly ILogger<FigLookupWorker<T>> _logger;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly TimeSpan _registrationDelay;
    private readonly HashSet<string> _validLookupTableNames;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;

    public FigLookupWorker(ILogger<FigLookupWorker<T>> logger, 
        IServiceScopeFactory serviceScopeFactory,
        IOptions<FigOptions> figOptions)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _registrationDelay = figOptions.Value.LookupTableRegistrationDelay;
        _validLookupTableNames = GetValidLookupTableNames();
    }
    
    
    public Task StartAsync(CancellationToken cancellationToken)
    {
        _stoppingCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        // Start the background task without blocking
        _executingTask = ExecuteAsync(_stoppingCts.Token);
        
        // Return immediately to allow other services to start
        return Task.CompletedTask;
    }

    private async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Lookup table registration will start after {Delay}", _registrationDelay);
            
            await Task.Delay(_registrationDelay, cancellationToken);
            
            await RegisterLookupTablesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown - ignore
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during lookup table registration");
        }
    }

    private async Task RegisterLookupTablesAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceScopeFactory.CreateScope();
        
        var lookupProviders = scope.ServiceProvider.GetServices<ILookupProvider>();
        var keyedLookupProviders = scope.ServiceProvider.GetServices<IKeyedLookupProvider>();

        foreach (var provider in lookupProviders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            // Only register lookup tables that have matching settings with [LookupTable] attributes
            if (_validLookupTableNames.Contains(provider.LookupName))
            {
                var items = await provider.GetItems();
                await RegisterLookupTable(provider.LookupName, items);
            }
        }

        foreach (var provider in keyedLookupProviders)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
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
        
        _logger.LogDebug("Lookup table registration completed");
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

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_executingTask == null)
        {
            return;
        }

        try
        {
            _stoppingCts?.Cancel();
        }
        finally
        {
            // Wait for the background task to complete or timeout
            var completedTask = await Task.WhenAny(_executingTask, Task.Delay(TimeSpan.FromSeconds(5), cancellationToken));
            if (completedTask == _executingTask)
            {
                await _executingTask; // Propagate any exceptions
            }
        }
    }

    public void Dispose()
    {
        if (_stoppingCts != null)
        {
            _stoppingCts.Cancel();
        
            // Wait for the task to complete (with a reasonable timeout)
            try
            {
                _executingTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Task was cancelled or faulted - expected during disposal
            }
        
            _stoppingCts.Dispose();
        }
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