namespace Fig.Common.Timer;

public class FigPeriodicTimer : IPeriodicTimer
{
    private readonly PeriodicTimer _timer;

    public FigPeriodicTimer(TimeSpan interval)
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