using System.Timers;

namespace Fig.Api.Utils;

public class Timer : ITimer
{
    private readonly System.Timers.Timer _timer; 
    
    public event EventHandler? Elapsed;

    public Timer(TimeSpan interval)
    {
        _timer = new System.Timers.Timer(interval.TotalMilliseconds);
        _timer.Elapsed += OnElapsed;
    }
    
    private void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        Elapsed?.Invoke(this, EventArgs.Empty);
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}