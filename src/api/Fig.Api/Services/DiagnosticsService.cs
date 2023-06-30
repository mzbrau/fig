using Fig.Common.Timer;

namespace Fig.Api.Services;

public class DiagnosticsService : IDiagnosticsService
{
    private readonly HashSet<long> _requests = new();
    private readonly IPeriodicTimer _timer;
    private readonly object _lockObject = new();
    private readonly DateTime _startTime = DateTime.UtcNow;

    public DiagnosticsService(ITimerFactory timerFactory)
    {
        _timer = timerFactory.Create(TimeSpan.FromSeconds(1));
        Task.Run(async () => await MonitorRequests());
    }

    private async Task MonitorRequests()
    {
        while (await _timer.WaitForNextTickAsync(CancellationToken.None))
        {
            var nowTicks = DateTime.UtcNow.Ticks;
            lock(_lockObject)
                _requests.RemoveWhere(a => a < nowTicks);
        }
    }
    
    public long TotalRequests { get; private set; }

    public double RequestsPerMinute
    {
        get
        {
            var numberOfMinutes = Math.Min(10, (DateTime.UtcNow - _startTime).TotalMinutes);
            lock (_lockObject)
                return Math.Round(_requests.Count / numberOfMinutes, 2);
        }
    }

    public void RegisterRequest()
    {
        TotalRequests++;
        var expiry = (DateTime.UtcNow + TimeSpan.FromMinutes(10)).Ticks;
        lock(_lockObject)
            _requests.Add(expiry);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}