using System.Diagnostics;
using System.Reflection;
using Fig.Client.Configuration;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Enums;

namespace Fig.Client.Versions;

internal class VersionProvider : IVersionProvider
{
    private readonly IFigConfigurationSource _config;
    private const string UnknownVersion = "Unknown";

    public VersionProvider(IFigConfigurationSource config)
    {
        _config = config;
    }

    public VersionProvider()
    {
        _config = new FigConfigurationSource();
    }

    public string GetFigVersion()
    {
        var assembly = Assembly.GetExecutingAssembly();
        return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
    }

    public string GetHostVersion()
    {
        if (!string.IsNullOrEmpty(_config.VersionOverride))
            return _config.VersionOverride!;

        var assembly = Assembly.GetEntryAssembly();

        if (assembly == null)
            return UnknownVersion;

        return _config.VersionType switch
        {
            VersionType.File => GetFileVersion(assembly),
            VersionType.Assembly => GetAssemblyVersion(assembly),
            VersionType.Product => GetProductVersion(assembly),
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

    private string GetProductVersion(Assembly assembly)
    {
        var fileVersionInfo = FileVersionInfo.GetVersionInfo(assembly.Location);
        return fileVersionInfo.ProductVersion ?? UnknownVersion;
    }
}