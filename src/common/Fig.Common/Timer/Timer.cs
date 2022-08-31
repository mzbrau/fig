﻿namespace Fig.Common.Timer;

public class Timer : ITimer
{
    private PeriodicTimer _timer;

    public Timer(TimeSpan interval)
    {
        _timer = new PeriodicTimer(interval);
    }

    public async ValueTask<bool> WaitForNextTickAsync(CancellationToken token)
    {
        return await _timer.WaitForNextTickAsync(token);
    }

    public void Dispose()
    {
        _timer.Dispose();
    }
}