namespace Fig.Api.Utils;

public interface ITimerFactory
{
    ITimer Create(TimeSpan interval);
}