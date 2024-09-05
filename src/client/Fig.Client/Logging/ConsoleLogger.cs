using System;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Logging;

public class ConsoleLogger : ILogger
{
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        Console.WriteLine($"{DateTime.Now} [{eventId}] {logLevel}: {formatter(state, exception)}");
    }

    public bool IsEnabled(LogLevel logLevel) => true;

#pragma warning disable CS8633
    public IDisposable BeginScope<TState>(TState state)
#pragma warning restore CS8633
    {
        return new Empty();
    }

    private class Empty : IDisposable
    {
        public void Dispose()
        {
        }
    }
}