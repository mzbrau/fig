namespace Fig.Common.Timer;

public interface ITimer : IDisposable
{
    ValueTask<bool> WaitForNextTickAsync(CancellationToken token);
}