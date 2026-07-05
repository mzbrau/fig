using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.Client.Configuration;

internal static class FigCommandLine
{
    internal const string DisableFigArg = "--disable-fig=true";
    internal const string InstanceArgPrefix = "--instance=";
    internal const string PrintAppSettingsArg = "--printappsettings";
    internal const string FigOfflineArg = "--figoffline";

    internal static Func<string[]?> CommandLineArgsProvider { get; set; } = Environment.GetCommandLineArgs;

    internal static bool IsFigDisabled()
    {
        return IsFigDisabled(CommandLineArgsProvider?.Invoke());
    }

    internal static bool IsFigDisabled(IEnumerable<string>? args)
    {
        return args?.Contains(DisableFigArg) == true;
    }

    internal static bool IsFigOffline(IEnumerable<string>? args)
    {
        return args?.Contains(FigOfflineArg) == true;
    }

    internal static string? GetInstanceOverride(IEnumerable<string>? args)
    {
        return args?
            .FirstOrDefault(a => a.StartsWith(InstanceArgPrefix, StringComparison.Ordinal))
            ?.Substring(InstanceArgPrefix.Length);
    }

    /// <summary>
    /// Parses key=value overrides that follow --printappsettings in the args list.
    /// Collects all args after the flag that look like key=value (contain '=') and don't start with '--'.
    /// </summary>
    internal static Dictionary<string, string> ParseAppSettingsOverrides(IEnumerable<string>? args)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (args == null)
            return result;

        var argList = args.ToList();
        var idx = argList.IndexOf(PrintAppSettingsArg);
        if (idx < 0)
            return result;

        for (var i = idx + 1; i < argList.Count; i++)
        {
            var arg = argList[i];
            if (arg.StartsWith("--", StringComparison.Ordinal))
                break;

            var eqPos = arg.IndexOf('=');
            if (eqPos <= 0)
                continue;

            var key = arg.Substring(0, eqPos);
            var value = arg.Substring(eqPos + 1).Trim('"');
            result[key] = value;
        }

        return result;
    }
}
