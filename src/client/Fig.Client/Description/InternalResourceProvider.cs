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
        string fullResourceName = resourceKey;

        // If the resource key doesn't contain a dot, try to find the full resource name
        if (!resourceKey.Contains('.'))
        {
            fullResourceName = FindResourceByFileName(assembly, resourceKey);
        }

        using var stream = assembly?.GetManifestResourceStream(fullResourceName);
        
        if (stream is null) 
            return string.Empty;
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
    
    public List<string> GetAllMarkdownResourceKeys()
    {
        var assembly = Assembly.GetEntryAssembly();

        return assembly?.GetManifestResourceNames()
            .Where(a => a.EndsWith(".md"))
            .Select(a => $"${a}")
            .ToList() ?? new List<string>();
    }
    
    public byte[]? GetImageResourceBytes(string imagePath)
    {
        var assembly = Assembly.GetEntryAssembly();

        var fullResourceName = GetEmbeddedResourceFilename(imagePath);

        using var stream = assembly?.GetManifestResourceStream(fullResourceName);
        
        if (stream is null) 
            return null;
        
        using var reader = new StreamReader(stream);
        using MemoryStream memoryStream = new MemoryStream();
        
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
    
    private string GetEmbeddedResourceFilename(string imagePath)
    {
        var allResources = GetAllNonMarkdownResourceKeys();
        var imageFilename = Path.GetFileName(imagePath);

        var matchingResource = allResources.FirstOrDefault(a => a.EndsWith(imageFilename));

        return matchingResource ?? imagePath;
    }
    
    private List<string> GetAllNonMarkdownResourceKeys()
    {
        var assembly = Assembly.GetEntryAssembly();

        return assembly?.GetManifestResourceNames()
            .Where(a => !a.EndsWith(".md"))
            .ToList() ?? [];
    }

    private string FindResourceByFileName(Assembly? assembly, string fileName)
    {
        var allResources = assembly?.GetManifestResourceNames() ?? [];
        
        return allResources.FirstOrDefault(r => r.EndsWith($".{fileName}.md")) ?? fileName;
    }
}