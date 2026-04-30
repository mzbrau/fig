using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.Abstractions.LookupTable;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;
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
    private readonly HashSet<string> _validLookupTableNames;
    private CancellationTokenSource? _stoppingCts;
    private Task? _executingTask;
    private bool _disposed;

    public FigLookupWorker(ILogger<FigLookupWorker<T>> logger, 
        IServiceScopeFactory serviceScopeFactory)
    {
        _logger = logger;
        _serviceScopeFactory = serviceScopeFactory;
        _validLookupTableNames = GetValidLookupTableNames();
    }

    [Obsolete("Use the constructor without IOptions<FigOptions>. The registration delay is now sourced from FigClientBridgeRegistry.")]
    public FigLookupWorker(ILogger<FigLookupWorker<T>> logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<FigOptions> figOptions)
        : this(logger, serviceScopeFactory)
    {
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
            var registrationDelay = FigClientBridgeRegistry.TryGet(typeof(T), out _, out var bridgeOptions)
                ? bridgeOptions.LookupTableRegistrationDelay
                : FigClientBridgeOptions.Default.LookupTableRegistrationDelay;

            _logger.LogDebug("Lookup table registration will start after {Delay}", registrationDelay);
            
            await Task.Delay(registrationDelay, cancellationToken);
            
            await RegisterLookupTablesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            // Graceful shutdown - ignore
        }
        catch (Exception ex)
        {
            _logger.LogError("Error during lookup table registration {ExceptionMessage}", ex.Message);
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
        
        if (FigClientBridgeRegistry.TryGet(typeof(T), out var bridge, out _))
            await bridge!.RegisterLookupTable(lookupTable);
        else
        {
            _logger.LogWarning("Fig client bridge is not available. Cannot register lookup table: {LookupName}", lookupName);
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
        catch (ObjectDisposedException)
        {
            // CTS was already disposed — expected if Dispose() ran first
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
        if (_disposed)
            return;

        _disposed = true;

        var cts = _stoppingCts;
        _stoppingCts = null;

        if (cts != null)
        {
            try
            {
                cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Already disposed — ignore
            }

            try
            {
                _executingTask?.Wait(TimeSpan.FromSeconds(5));
            }
            catch (AggregateException)
            {
                // Task was cancelled or faulted - expected during disposal
            }

            cts.Dispose();
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
