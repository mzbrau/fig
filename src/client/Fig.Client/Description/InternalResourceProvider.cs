using System.IO;
using System.Reflection;

namespace Fig.Client.Description;

public class InternalResourceProvider : IInternalResourceProvider
{
    public string GetStringResource(string resourceKey)
    {
        var assembly = Assembly.GetEntryAssembly();

        using var stream = assembly?.GetManifestResourceStream(resourceKey);
        
        if (stream is null) 
            return resourceKey;
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}