using System.Reflection;

namespace Fig.Common.MarkdownUtils.Description;

public class InternalResourceProvider : IInternalResourceProvider
{
    public string GetStringResource(string resourceKey, Assembly? assemblyWithResource = null)
    {
        var assembly = assemblyWithResource ?? Assembly.GetEntryAssembly();

        using var stream = assembly?.GetManifestResourceStream(resourceKey);
        
        if (stream is null) 
            return resourceKey;
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}