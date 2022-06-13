namespace Fig.Api.ApiStatus;

public interface IApiStatusMonitor : IDisposable
{
    void Start();
}