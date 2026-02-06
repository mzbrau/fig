using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client;

namespace Fig.LoadTest;

public sealed class LoadTestRunner : IDisposable
{
    private readonly LoadTestOptions _options;
    private readonly IReadOnlyList<LoadTestClientDefinition> _definitions;
    private readonly StatusSyncMetrics _metrics = new();
    private readonly List<LoadTestClient> _clients = new();
    private readonly List<Thread> _threads = new();
    private readonly CancellationTokenSource _cts = new();

    public LoadTestRunner(LoadTestOptions options, IReadOnlyList<LoadTestClientDefinition> definitions)
    {
        _options = options;
        _definitions = definitions;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("Starting Fig load test.");
        Console.WriteLine($"API: {_options.ApiUri}");
        Console.WriteLine($"Duration: {_options.Duration}");
        Console.WriteLine($"Clients: {_definitions.Count}");

        await RegisterClientsAsync();

        _cts.CancelAfter(_options.Duration);
        StartClientThreads();
        WaitForCompletion();

        var summary = _metrics.Snapshot();
        Console.WriteLine("\nSync Status Performance (milliseconds):");
        Console.WriteLine($"Min: {summary.Min.TotalMilliseconds:F2}");
        Console.WriteLine($"Max: {summary.Max.TotalMilliseconds:F2}");
        Console.WriteLine($"Avg: {summary.Average.TotalMilliseconds:F2}");
        Console.WriteLine($"Count: {summary.Count}");
    }

    private async Task RegisterClientsAsync()
    {
        var uniqueClients = _definitions
            .Select(d => new { d.ClientName, d.SettingsType })
            .Distinct();

        foreach (var client in uniqueClients)
        {
            var settings = CreateSettings(client.SettingsType);
            using var loadClient = new LoadTestClient(
                new LoadTestClientDefinition(client.ClientName, "registration", client.SettingsType),
                _options);
            await loadClient.RegisterAsync(settings);
            Console.WriteLine($"Registered {client.ClientName}.");
        }
    }

    private void StartClientThreads()
    {
        foreach (var definition in _definitions)
        {
            var client = new LoadTestClient(definition, _options);
            _clients.Add(client);

            var thread = new Thread(() => RunClientLoop(client, _cts.Token))
            {
                IsBackground = true,
                Name = $"{definition.ClientName}-{definition.InstanceName}"
            };

            _threads.Add(thread);
            thread.Start();

            if (_options.StaggerMilliseconds > 0)
                Thread.Sleep(_options.StaggerMilliseconds);
        }
    }

    private void RunClientLoop(LoadTestClient client, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                var duration = client.SyncStatusAsync().GetAwaiter().GetResult();
                _metrics.Record(duration);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Sync failed for {client.ClientName}/{client.InstanceName}: {ex.Message}");
            }

            SleepUntilNextInterval(token);
        }
    }

    private void SleepUntilNextInterval(CancellationToken token)
    {
        var wait = _options.SyncInterval;
        var start = DateTime.UtcNow;
        while (!token.IsCancellationRequested)
        {
            var elapsed = DateTime.UtcNow - start;
            var remaining = wait - elapsed;
            if (remaining <= TimeSpan.Zero)
                break;

            Thread.Sleep(remaining < TimeSpan.FromMilliseconds(100) ? remaining : TimeSpan.FromMilliseconds(100));
        }
    }

    private void WaitForCompletion()
    {
        foreach (var thread in _threads)
        {
            thread.Join();
        }
    }

    private static SettingsBase CreateSettings(Type settingsType)
    {
        return (SettingsBase)Activator.CreateInstance(settingsType)!;
    }

    public void Dispose()
    {
        foreach (var client in _clients)
            client.Dispose();

        _cts.Dispose();
    }
}
