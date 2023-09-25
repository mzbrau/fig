using Microsoft.Extensions.Logging;

namespace Fig.Client.Logging;

public class SimpleLoggerFactory : ILoggerFactory
{
    public void Dispose()
    {
    }

    public ILogger CreateLogger(string categoryName)
    {
        return new ConsoleLogger();
    }

    public void AddProvider(ILoggerProvider provider)
    {
    }
}