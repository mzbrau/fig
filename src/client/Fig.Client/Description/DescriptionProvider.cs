using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

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
            var resourceValue = GetResource(resource);
            
            if (!first && !string.IsNullOrWhiteSpace(resourceValue))
                AddDivider(builder);
            
            builder.AppendLine(resourceValue);
            if (!string.IsNullOrWhiteSpace(resourceValue))
                first = false;
        }

        return builder.ToString();
    }

    public List<string> GetAllMarkdownResourceKeys()
    {
        return _internalResourceProvider.GetAllMarkdownResourceKeys();
    }

    private void AddDivider(StringBuilder builder)
    {
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
    }

    private string GetResource(string resourceKey)
    {
        var resourceKeyParts = resourceKey.Split('#');

        try
        {
            var markdownResource = _internalResourceProvider.GetStringResource(resourceKeyParts[0].TrimStart('$'));

            if (resourceKeyParts.Length > 1)
            {
                var extractedSection = _markdownExtractor.ExtractSection(markdownResource, resourceKeyParts[1]);
                markdownResource = ProcessMarkdown(extractedSection);
            }
            else
            {
                markdownResource = ProcessMarkdown(markdownResource);
            }

            return markdownResource;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error while trying to read or process resource with key {resourceKey}. {ex.Message}");
            return resourceKey;
        }
    }

    private string ProcessMarkdown(string markdownContent)
    {
        markdownContent = StripFrontMatter(markdownContent);
        markdownContent = StripInternalAdmonitions(markdownContent);
        markdownContent = EmbedImages(markdownContent);
        markdownContent = RemoveInternalLinks(markdownContent);
        return markdownContent;
    }

    private string RemoveInternalLinks(string markdownContent)
    {
        // Regex to match markdown links: [text](url)
        // Internal links are assumed to be relative (not starting with http, https, or mailto)
        var linkRegex = new Regex(@"\[(.*?)\]\((?!https?:|mailto:)(.*?)\)");
        return linkRegex.Replace(markdownContent, m => $"**{m.Groups[1].Value}**");
    }
    
    private string EmbedImages(string markdownContent)
    {
        // Regex pattern to match image markdown syntax ![alt text](image url)
        var imageRegex = new Regex(@"!\[.*?\]\((.*?)\)", RegexOptions.IgnoreCase);
        
        var matches = imageRegex.Matches(markdownContent);

        foreach (Match match in matches)
        {
            var imageUrl = match.Groups[1].Value;
            
            if (!IsBase64String(imageUrl))
            {
                var imageName = Path.GetFileName(imageUrl);
                var base64String = GetBase64String(imageName);
                markdownContent = markdownContent.Replace(imageUrl, base64String);
            }
        }

        return markdownContent;
    }

    private bool IsBase64String(string s)
    {
        return (s.Length % 4 == 0) && Regex.IsMatch(s, @"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.None);
    }

    private string GetBase64String(string imageName)
    {
        var imageBytes = _internalResourceProvider.GetImageResourceBytes(imageName);

        if (imageBytes is null)
            return imageName;

        var extension = Path.GetExtension(imageName).ToLowerInvariant();
        string base64String;

        // Handle SVG as text and PNG as binary
        if (extension == ".svg")
        {
            // Convert the byte array to a string (UTF-8 encoded SVG content)
            var svgContent = Encoding.UTF8.GetString(imageBytes);
            base64String = Convert.ToBase64String(Encoding.UTF8.GetBytes(svgContent));
            return $"data:image/svg+xml;base64,{base64String}";
        }

        base64String = Convert.ToBase64String(imageBytes);
        return $"data:image/png;base64,{base64String}";
    }

    private string StripFrontMatter(string markdownContent)
    {
        var frontMatterRegex = new Regex(@"^---[\s\S]*?---\s*", RegexOptions.Multiline);
        return frontMatterRegex.Replace(markdownContent, string.Empty);
    }

    private string StripInternalAdmonitions(string markdownContent)
    {
        var internalAdmonitionRegex = new Regex(@":::internal[\s\S]*?:::", RegexOptions.Multiline);
        return internalAdmonitionRegex.Replace(markdownContent, string.Empty);
    }
}