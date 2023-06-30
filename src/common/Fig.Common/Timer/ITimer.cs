namespace Fig.Common.Timer;

public interface ITimer : IDisposable
{
    void Start();

    void Stop();
}