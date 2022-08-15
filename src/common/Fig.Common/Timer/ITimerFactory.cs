namespace Fig.Common.Timer;

public interface ITimerFactory
{
    ITimer Create(TimeSpan interval);
}