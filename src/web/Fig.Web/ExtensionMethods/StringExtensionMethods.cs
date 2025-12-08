using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using Markdown.ColorCode;
using Microsoft.AspNetCore.Components;
using Fig.Web.Constants;

namespace Fig.Web.ExtensionMethods;

public static class StringExtensionMethods
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseCustomContainers() // Add this line to support custom containers for Admonitions
        .UseColorCode()
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
            // Test the regex with a timeout to ensure it's valid and doesn't cause catastrophic backtracking
            // ReSharper disable once ReturnValueOfPureMethodIsNotUsed
            Regex.Match(string.Empty, pattern, RegexOptions.None, RegexConstants.DefaultTimeout);
        }
        catch (ArgumentException)
        {
            return false;
        }
        catch (RegexMatchTimeoutException)
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
        var document = Markdig.Markdown.Parse(markdown, Pipeline);

        foreach (var descendant in document.Descendants())
        {
            if (descendant is AutolinkInline or LinkInline)
            {
                descendant.GetAttributes().AddPropertyIfNotExist("target", "_blank");
            }
            
            // Add Bootstrap table classes to all table elements
            if (descendant is Table table)
            {
                table.GetAttributes().AddClass("table table-striped table-bordered table-hover");
            }

            if (descendant is CodeBlock codeBlock)
            {
                codeBlock.GetAttributes().AddClass("code-block");
            }

            if (descendant is CodeInline)
            {
                descendant.GetAttributes().AddClass("inline-code");
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

        // Use a regular expression to split camel case into words with timeout.
        string pattern = "(?<=[a-z])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])";
        return Regex.Replace(input, pattern, " ", RegexOptions.None, RegexConstants.DefaultTimeout);
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

        // Remove images: ![alt text](url) - with timeout
        string noImages = Regex.Replace(markdown, @"!\[.*?\]\(.*?\)", string.Empty, RegexOptions.None, RegexConstants.DefaultTimeout);

        // Simplify links: [text](url) → text - with timeout
        string simplifiedLinks = Regex.Replace(noImages, @"\[(.*?)\]\(.*?\)", "$1", RegexOptions.None, RegexConstants.DefaultTimeout);

        return simplifiedLinks;
    }
}