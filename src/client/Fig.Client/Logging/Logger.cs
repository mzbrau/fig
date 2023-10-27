#nullable enable
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace Fig.Client.Logging;

internal static class Logger
{
    private static ILoggerFactory? _factory;

    public static ILoggerFactory LoggerFactory
    {
        get => _factory ??= new NullLoggerFactory();
        set => _factory = value;
    }

    public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();
}