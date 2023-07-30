using System;

namespace Fig.Client.Description;

public class DescriptionProvider : IDescriptionProvider
{
    private readonly IInternalResourceProvider _internalResourceProvider;
    private readonly IMarkdownExtractor _markdownExtractor;

    public DescriptionProvider(IInternalResourceProvider internalResourceProvider, IMarkdownExtractor markdownExtractor)
    {
        _internalResourceProvider = internalResourceProvider;
        _markdownExtractor = markdownExtractor;
    }
    
    public string GetDescription(string descriptionOrKey)
    {
        if (!descriptionOrKey.StartsWith("$"))
            return descriptionOrKey;

        var resourceKeyParts = descriptionOrKey.Split('#');

        try
        {
            var markdownResource = _internalResourceProvider.GetStringResource(resourceKeyParts[0].TrimStart('$'));

            return resourceKeyParts.Length > 1
                ? _markdownExtractor.ExtractSection(markdownResource, resourceKeyParts[1])
                : markdownResource;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while trying to read or process resource with key {descriptionOrKey}. {ex.Message}");
            return descriptionOrKey;
        }
    }
}