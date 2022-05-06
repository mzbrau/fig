using System.Reflection;
using Fig.Client.Configuration;

namespace Fig.Client.Versions
{
    public class VersionProvider : IVersionProvider
    {
        private readonly IFigOptions _options;

        public VersionProvider(IFigOptions options)
        {
            _options = options;
        }

        public string GetFigVersion()
        {
            var assembly = Assembly.GetExecutingAssembly();
            return assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
        }

        public string GetHostVersion()
        {
            if (!string.IsNullOrEmpty(_options.VersionOverride))
                return _options.VersionOverride;
            
            var assembly = Assembly.GetEntryAssembly();

            if (assembly == null)
                return "Unknown";

            var version = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>().Version;
            return version;
        }
    }
}