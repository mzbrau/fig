namespace Fig.Api.Utils;

public interface ITimer : IDisposable
{
    ValueTask<bool> WaitForNextTickAsync(CancellationToken token);
}