namespace Fig.Api.Utils;

public class Timer : ITimer
{
    private readonly PeriodicTimer _timer;

    public Timer(TimeSpan interval)
    {
        _timer = new PeriodicTimer(interval);
    }

    public async ValueTask<bool> WaitForNextTickAsync(CancellationToken token)
    {
        return await _timer.WaitForNextTickAsync(token);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}