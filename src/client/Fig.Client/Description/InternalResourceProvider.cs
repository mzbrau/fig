using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Fig.Client.Description;

internal class InternalResourceProvider : IInternalResourceProvider
{
    public string GetStringResource(string resourceKey)
    {
        var assembly = Assembly.GetEntryAssembly();

        using var stream = assembly?.GetManifestResourceStream(resourceKey);
        
        if (stream is null) 
            return string.Empty;
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    public List<string> GetAllResourceKeys()
    {
        var assembly = Assembly.GetEntryAssembly();

        return assembly?.GetManifestResourceNames()
            .Where(a => a.EndsWith(".md"))
            .Select(a => $"${a}")
            .ToList() ?? new List<string>();
    }
}