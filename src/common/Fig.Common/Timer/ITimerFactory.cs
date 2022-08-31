namespace Fig.Common.Timer;

public interface ITimerFactory
{
    IPeriodicTimer Create(TimeSpan interval);

    ITimer Create(Func<Task> action, TimeSpan interval);
}