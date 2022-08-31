namespace Fig.Common.Timer;

public class TimerFactory : ITimerFactory
{
    public IPeriodicTimer Create(TimeSpan interval)
    {
        return new FigPeriodicTimer(interval);
    }

    public ITimer Create(Func<Task> action, TimeSpan interval)
    {
        return new FigTimer(action, interval);
    }
}
