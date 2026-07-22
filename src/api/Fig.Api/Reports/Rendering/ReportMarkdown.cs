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
            // ImageInline is represented as LinkInline with IsImage=true — leave data: images alone.
            if (descendant is LinkInline { IsImage: false } link)
            {
                if (!IsSafeLinkUrl(link.Url))
                    link.Url = string.Empty;

                var attrs = link.GetAttributes();
                attrs.AddPropertyIfNotExist("target", "_blank");
                attrs.AddPropertyIfNotExist("rel", "noopener noreferrer");
            }
            else if (descendant is AutolinkInline autolink)
            {
                if (!IsSafeLinkUrl(autolink.Url))
                    autolink.Url = string.Empty;

                var attrs = autolink.GetAttributes();
                attrs.AddPropertyIfNotExist("target", "_blank");
                attrs.AddPropertyIfNotExist("rel", "noopener noreferrer");
            }

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

    internal static bool IsSafeLinkUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return false;

        var trimmed = url.Trim();
        if (trimmed.StartsWith('#') || trimmed.StartsWith('/'))
            return true;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return false;

        return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;
    }
}
