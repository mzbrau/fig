using System.Diagnostics;
using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Extensions.Tables;
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

    private static long _descriptionHtmlElapsedMs;

    public static void ResetDescriptionHtmlTiming() =>
        Interlocked.Exchange(ref _descriptionHtmlElapsedMs, 0);

    public static long TakeDescriptionHtmlElapsedMs() =>
        Interlocked.Exchange(ref _descriptionHtmlElapsedMs, 0);

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
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (string.IsNullOrEmpty(markdown))
                return string.Empty;

            if (!LooksLikeMarkdown(markdown))
                return WebUtility.HtmlEncode(markdown);

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
        finally
        {
            Interlocked.Add(ref _descriptionHtmlElapsedMs, stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// True when the text likely contains markdown syntax worth running through Markdig.
    /// </summary>
    public static bool LooksLikeMarkdown(string text)
    {
        // Prefer distinctive markers over '-' / '_' which appear in ordinary prose.
        return text.Contains('#') ||
               text.Contains('`') ||
               text.Contains('*') ||
               text.Contains('[') ||
               text.Contains(']') ||
               text.Contains('|') ||
               text.Contains('>') ||
               text.Contains('~') ||
               text.Contains('\\') ||
               text.Contains("![");
    }
    
    public static string SplitCamelCase(this string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return input;

        var length = input.Length;
        if (length < 2)
            return input;

        var buffer = new char[length * 2];
        var outIndex = 0;
        buffer[outIndex++] = input[0];

        for (var i = 1; i < length; i++)
        {
            var previous = input[i - 1];
            var current = input[i];
            var nextIsLower = i + 1 < length && char.IsLower(input[i + 1]);

            // Insert space between lower→Upper or between acronym end and next word (XMLParser → XML Parser).
            if ((char.IsLower(previous) && char.IsUpper(current)) ||
                (char.IsUpper(previous) && char.IsUpper(current) && nextIsLower))
            {
                buffer[outIndex++] = ' ';
            }

            buffer[outIndex++] = current;
        }

        return new string(buffer, 0, outIndex);
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