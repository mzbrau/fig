using System.Net;
using System.Text.RegularExpressions;
using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Fig.Api.Reports.Rendering;

public static class ReportMarkdown
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .DisableHtml()
        .Build();

    private static readonly Regex MarkdownListMarkerRegex = new(
        @"^\s*(?:[-+]\s|\d+\.\s)",
        RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.CultureInvariant);

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrEmpty(markdown))
            return string.Empty;

        if (!LooksLikeMarkdown(markdown))
            return WebUtility.HtmlEncode(markdown);

        var document = Markdown.Parse(markdown, Pipeline);

        foreach (var descendant in document.Descendants())
        {
            if (descendant is AutolinkInline or LinkInline)
                descendant.GetAttributes().AddPropertyIfNotExist("target", "_blank");

            if (descendant is CodeBlock)
                descendant.GetAttributes().AddClass("report-code-block");

            if (descendant is CodeInline)
                descendant.GetAttributes().AddClass("report-inline-code");
        }

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        Pipeline.Setup(renderer);
        renderer.Render(document);
        return writer.ToString();
    }

    public static bool LooksLikeMarkdown(string text)
    {
        return text.Contains('#') ||
               text.Contains('`') ||
               text.Contains('*') ||
               text.Contains('[') ||
               text.Contains(']') ||
               text.Contains('|') ||
               text.Contains('>') ||
               text.Contains('~') ||
               text.Contains('\\') ||
               text.Contains("![") ||
               MarkdownListMarkerRegex.IsMatch(text);
    }
}
