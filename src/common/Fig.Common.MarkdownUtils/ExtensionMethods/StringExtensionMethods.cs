using Markdig;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Fig.Common.MarkdownUtils.ExtensionMethods;

public static class StringExtensionMethods
{
    /// <summary>
    /// Converts markdown to html and ensures links are opened in new windows.
    /// </summary>
    /// <param name="markdown">The markdown string to convert.</param>
    /// <returns>The html for the markdown document</returns>
    public static string ToHtml(this string markdown)
    {
        var pipeline = new MarkdownPipelineBuilder().Build();
        var document = Markdown.Parse(markdown, pipeline);

        foreach (var descendant in document.Descendants())
        {
            if (descendant is AutolinkInline or LinkInline)
            {
                descendant.GetAttributes().AddPropertyIfNotExist("target", "_blank");
            }
        }

        using var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        pipeline.Setup(renderer);
        renderer.Render(document);

        return writer.ToString();
    }
}