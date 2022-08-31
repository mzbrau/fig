using System.Timers;

namespace Fig.Common.Timer;

public class FigTimer : ITimer
{
    private readonly Func<Task> _action;
    private readonly System.Timers.Timer _timer;

    public FigTimer(Func<Task> action, TimeSpan interval)
    {
        _action = action;
        _timer = new System.Timers.Timer(interval.TotalMilliseconds);
        _timer.Elapsed += OnTimerElapsed;
    }

    public void Start()
    {
        _timer.Start();
    }
    
    public void Stop()
    {
        _timer.Stop();
    }

    private async void OnTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        await _action();
    }
}