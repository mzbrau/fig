using System.Reflection;

namespace Fig.Common;

public class VersionHelper : IVersionHelper
{
    private const string UnknownVersion = "Unknown";
    private string? _version;
    
    public string GetVersion()
    {
        if (_version != null)
            return _version;
        
        var assembly = Assembly.GetEntryAssembly();

        if (assembly == null)
            return UnknownVersion;

        var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
        _version = version ?? UnknownVersion;
        return _version;
    }
}