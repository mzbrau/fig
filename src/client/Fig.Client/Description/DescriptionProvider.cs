using System;
using System.Linq;
using System.Text;

namespace Fig.Client.Description;

internal class DescriptionProvider : IDescriptionProvider
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

        var resources = descriptionOrKey.Split(',').Select(a => a.Trim());

        var builder = new StringBuilder();
        var first = true;
        foreach (var resource in resources)
        {
            if (!first)
                AddDivider(builder);

            builder.AppendLine(GetResource(resource));
            first = false;
        }

        return builder.ToString();
    }

    private void AddDivider(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
    }

    string GetResource(string resourceKey)
    {
        var resourceKeyParts = resourceKey.Split('#');

        try
        {
            var markdownResource = _internalResourceProvider.GetStringResource(resourceKeyParts[0].TrimStart('$'));

            return resourceKeyParts.Length > 1
                ? _markdownExtractor.ExtractSection(markdownResource, resourceKeyParts[1])
                : markdownResource;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while trying to read or process resource with key {resourceKey}. {ex.Message}");
            return resourceKey;
        }
    }
}