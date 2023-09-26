using System.IO;
using System.Linq;
using Markdig;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Fig.Client.Description;

public class MarkdownExtractor : IMarkdownExtractor
{
    public string ExtractSection(string markdown, string desiredHeading)
    {
        var document = Markdown.Parse(markdown);
        
        int? selectedHeadingLevel = null;

        foreach (var block in document.ToList()) {
            if (selectedHeadingLevel is null) {
                if (block is HeadingBlock { Inline.FirstChild: LiteralInline literalInline } h && 
                    literalInline.Content.ToString().Equals(desiredHeading, System.StringComparison.InvariantCultureIgnoreCase)) {
                    selectedHeadingLevel = h.Level;
                }

                document.Remove(block);
                continue;
            }

            if (block is HeadingBlock heading && heading.Level <= selectedHeadingLevel) {
                selectedHeadingLevel = null;
                document.Remove(block);
            }
        }

        return ConvertToMarkdownString(document);
    }

    private string ConvertToMarkdownString(MarkdownDocument document)
    {
        var writer = new StringWriter();
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();
        var renderer = new NormalizeRenderer(writer);
        pipeline.Setup(renderer);
        renderer.Render(document);
        return writer.ToString();
    }
}