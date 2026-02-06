using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.LoadTest;

public sealed class LoadTestOptions
{
    private const int DefaultStaggerMilliseconds = 150;

    public LoadTestOptions(Uri apiUri, string clientSecret, TimeSpan duration, int staggerMilliseconds)
    {
        ApiUri = apiUri;
        ClientSecret = clientSecret;
        Duration = duration;
        StaggerMilliseconds = staggerMilliseconds;
    }

    public Uri ApiUri { get; }
    public string ClientSecret { get; }
    public TimeSpan Duration { get; }
    public int StaggerMilliseconds { get; }
    public TimeSpan SyncInterval { get; } = TimeSpan.FromSeconds(2);

    public static LoadTestOptions Parse(string[] args)
    {
        var arguments = ParseArgs(args);
        var duration = ParseDuration(arguments);
        var apiUri = ParseApiUri(arguments);
        var clientSecret = "b0356148ed244ac49223b90dcf236dde";
        var staggerMilliseconds = ParseStaggerMilliseconds(arguments);

        return new LoadTestOptions(apiUri, clientSecret, duration, staggerMilliseconds);
    }

    private static Dictionary<string, string> ParseArgs(IEnumerable<string> args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var arg in args)
        {
            if (!arg.StartsWith("--", StringComparison.Ordinal))
                continue;

            var parts = arg.Split('=', 2, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 2)
            {
                result[parts[0].Trim()] = parts[1].Trim();
            }
        }

        return result;
    }

    private static TimeSpan ParseDuration(IReadOnlyDictionary<string, string> arguments)
    {
        if (arguments.TryGetValue("--duration", out var value) && TimeSpan.TryParse(value, out var duration))
        {
            return duration;
        }

        return TimeSpan.FromMinutes(5);
    }

    private static Uri ParseApiUri(IReadOnlyDictionary<string, string> arguments)
    {
        if (arguments.TryGetValue("--api", out var apiValue))
        {
            return ValidateUri(apiValue);
        }

        var env = Environment.GetEnvironmentVariable("FIG_API_URI");
        if (!string.IsNullOrWhiteSpace(env))
        {
            var first = env.Split(',').Select(a => a.Trim()).FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(first))
                return ValidateUri(first);
        }

        throw new InvalidOperationException("Missing Fig API URI. Provide --api=https://localhost:7281 or set FIG_API_URI.");
    }

    private static int ParseStaggerMilliseconds(IReadOnlyDictionary<string, string> arguments)
    {
        if (arguments.TryGetValue("--staggerMs", out var value) && int.TryParse(value, out var result))
        {
            return Math.Max(0, result);
        }

        return DefaultStaggerMilliseconds;
    }

    private static Uri ValidateUri(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
            throw new InvalidOperationException($"Invalid Fig API URI: {value}");

        return uri;
    }
}
