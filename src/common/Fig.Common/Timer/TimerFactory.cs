namespace Fig.Common.Timer;

public class TimerFactory : ITimerFactory
{
    public ITimer Create(TimeSpan interval)
    {
        return new Timer(interval);
    }
}