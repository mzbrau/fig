using System.Reflection;
using Fig.Client.Configuration;

namespace Fig.Client.Versions;

internal class VersionProvider : IVersionProvider
{
    private readonly FigOptions _options;

    public VersionProvider(FigOptions options)
    {
        _options = options;
    }

    public VersionProvider()
    {
        _options = new FigOptions();
    }

    public string GetFigVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
    }

    public string GetHostVersion()
    {
        if (!string.IsNullOrEmpty(_options.VersionOverride))
            return _options.VersionOverride!;

        var assembly = Assembly.GetEntryAssembly();

        if (assembly == null)
            return "Unknown";

        var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        return version;
    }
}