using System.Diagnostics;
using System.Reflection;
using Fig.Client.Configuration;
using Fig.Client.Enums;

namespace Fig.Client.Versions;

internal class VersionProvider : IVersionProvider
{
    private readonly FigOptions _options;
    private const string UnknownVersion = "Unknown";

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
            return UnknownVersion;

        return _options.VersionType switch
        {
            VersionType.File => GetFileVersion(assembly),
            VersionType.Assembly => GetAssemblyVersion(assembly),
            _ => UnknownVersion
        };
    }
    
    private string GetFileVersion(Assembly assembly)
    {
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        return fileVersionInfo.FileVersion ?? UnknownVersion;
    }

    private string GetAssemblyVersion(Assembly assembly)
    {
        var assemblyName = AssemblyName.GetAssemblyName(assembly.Location);
        return assemblyName?.Version?.ToString() ?? UnknownVersion;
    }
}