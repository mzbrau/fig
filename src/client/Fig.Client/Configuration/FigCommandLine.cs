using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.Client.Configuration;

internal static class FigCommandLine
{
    internal const string DisableFigArg = "--disable-fig=true";

    internal static Func<string[]?> CommandLineArgsProvider { get; set; } = Environment.GetCommandLineArgs;

    internal static bool IsFigDisabled()
    {
        return IsFigDisabled(CommandLineArgsProvider?.Invoke());
    }

    internal static bool IsFigDisabled(IEnumerable<string>? args)
    {
        return args?.Contains(DisableFigArg) == true;
    }
}
