using System.Text.Json;
using Yarp.ReverseProxy.Configuration;

namespace Fig.Examples.Yarp;

public class ReverseProxyConfigLogger : IHostedService, IDisposable
{
    private readonly IProxyConfigProvider _configProvider;
    private readonly ILogger<ReverseProxyConfigLogger> _logger;
    private IDisposable? _changeToken;

    public ReverseProxyConfigLogger(
        IProxyConfigProvider configProvider,
        ILogger<ReverseProxyConfigLogger> logger)
    {
        _configProvider = configProvider;
        _logger = logger;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Log initial configuration
        LogConfiguration("Initial configuration loaded");

        // Subscribe to configuration changes
        SubscribeToChanges();

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    private void SubscribeToChanges()
    {
        var config = _configProvider.GetConfig();
        _changeToken = config.ChangeToken.RegisterChangeCallback(_ =>
        {
            LogConfiguration("Configuration changed");
            
            // Re-subscribe to future changes
            SubscribeToChanges();
        }, null);
    }

    private void LogConfiguration(string message)
    {
        var config = _configProvider.GetConfig();

        _logger.LogInformation("=== {Message} ===", message);

        // Log routes
        var routes = config.Routes?.ToList() ?? new List<RouteConfig>();
        _logger.LogInformation("Routes ({Count}):", routes.Count);
        
        foreach (var route in routes)
        {
            var routeJson = JsonSerializer.Serialize(new
            {
                route.RouteId,
                route.ClusterId,
                route.Match,
                route.Order,
                route.AuthorizationPolicy,
                route.CorsPolicy,
                route.Metadata,
                Transforms = route.Transforms?.Select(t => t.ToDictionary(kvp => kvp.Key, kvp => kvp.Value))
            }, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("  Route '{RouteId}':\n{RouteConfig}", route.RouteId, routeJson);
        }

        // Log clusters
        var clusters = config.Clusters?.ToList() ?? new List<ClusterConfig>();
        _logger.LogInformation("Clusters ({Count}):", clusters.Count);
        
        foreach (var cluster in clusters)
        {
            var clusterJson = JsonSerializer.Serialize(new
            {
                cluster.ClusterId,
                Destinations = cluster.Destinations?.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new { kvp.Value.Address, kvp.Value.Health, kvp.Value.Metadata }),
                cluster.HealthCheck,
                cluster.HttpClient,
                cluster.HttpRequest,
                cluster.LoadBalancingPolicy,
                cluster.SessionAffinity,
                cluster.Metadata
            }, new JsonSerializerOptions { WriteIndented = true });
            
            _logger.LogInformation("  Cluster '{ClusterId}':\n{ClusterConfig}", cluster.ClusterId, clusterJson);
        }

        _logger.LogInformation("=== End of configuration ===");
    }

    public void Dispose()
    {
        _changeToken?.Dispose();
    }
}
