namespace Fig.Api.Utils;

public interface ITimer : IDisposable
{
    event EventHandler Elapsed;

    void Start();

    void Stop();
}