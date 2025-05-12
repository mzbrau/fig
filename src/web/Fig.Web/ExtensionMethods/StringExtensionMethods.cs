using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Microsoft.AspNetCore.Components;

namespace Fig.Web.ExtensionMethods;

public static class StringExtensionMethods
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseCustomContainers() // Add this line to support custom containers for Admonitions
        .DisableHtml()
        .Build();

    public static string? QueryString(this NavigationManager navigationManager, string key)
    {
        return navigationManager.QueryString()[key];
    }
    
    public static bool IsValidRegex(this string pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern)) 
            return false;

        try
        {
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Regex.Match(string.Empty, pattern);
        }
        catch (ArgumentException)
        {
            return false;
        }

        return true;
    }
    
    /// <summary>
    /// Converts markdown to html and ensures links are opened in new windows.
    /// </summary>
    /// <param name="markdown">The markdown string to convert.</param>
    /// <returns>The html for the markdown document</returns>
    public static string ToHtml(this string markdown)
    {
        var document = Markdown.Parse(markdown, Pipeline);

        foreach (var descendant in document.Descendants())
        {
            if (descendant is AutolinkInline or LinkInline)
            {
                descendant.GetAttributes().AddPropertyIfNotExist("target", "_blank");
            }
        }

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);

        return writer.ToString();
    }
    
    public static string SplitCamelCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Use a regular expression to split camel case into words.
        string pattern = "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])";
        return Regex.Replace(input, pattern, " ");
    }

    public static string RemoveNewLines(this string input)
    {
        return input.Replace("\n", string.Empty).Replace("\r", string.Empty);
    }
    
    public static string EscapeAndQuote(this string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return "\"\"";
        }

        // Escape double quotes by doubling them and wrap the value in double quotes
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }
    
    public static string StripImagesAndSimplifyLinks(this string markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return markdown;

        // Remove images: ![alt text](url)
        string noImages = Regex.Replace(markdown, @"!\[.*?\]\(.*?\)", string.Empty);

        // Simplify links: [text](url) → text
        string simplifiedLinks = Regex.Replace(noImages, @"\[(.*?)\]\(.*?\)", "$1");

        return simplifiedLinks;
    }
}