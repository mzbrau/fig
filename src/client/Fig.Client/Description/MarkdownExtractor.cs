using System.IO;
using System.Text;
using Markdig;
using Markdig.Renderers.Normalize;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;

namespace Fig.Client.Description;

internal class MarkdownExtractor : IMarkdownExtractor
{
    public string ExtractSection(string markdown, string desiredHeading)
    {
        var document = Markdown.Parse(markdown);

        // Locate the selected heading and its index/level
        var selectedIndex = -1;
        var selectedLevel = -1;

        for (var i = 0; i < document.Count; i++)
        {
            if (document[i] is HeadingBlock hb)
            {
                var headingText = GetHeadingText(hb);
                if (headingText.Equals(desiredHeading, System.StringComparison.InvariantCultureIgnoreCase))
                {
                    selectedIndex = i;
                    selectedLevel = hb.Level;
                    break;
                }
            }
        }

        // If not found, return empty string
        if (selectedIndex == -1)
            return string.Empty;

        // Determine where the extracted section should end.
        // Primary rule: stop at the next heading with level <= selected level (same or higher priority)
        // Fallback: if there is no such heading, stop at the next heading of any level.
        int endExclusive = document.Count; // by default, include to end

        // Find next heading with level <= selected
        var boundaryFound = false;
        for (var i = selectedIndex + 1; i < document.Count; i++)
        {
            if (document[i] is HeadingBlock hb)
            {
                if (hb.Level <= selectedLevel)
                {
                    endExclusive = i;
                    boundaryFound = true;
                    break;
                }
            }
        }

        if (!boundaryFound)
        {
            // Fallback: stop at the very next heading of any level (exclude sub-sections when there is no sibling/parent boundary)
            for (var i = selectedIndex + 1; i < document.Count; i++)
            {
                if (document[i] is HeadingBlock)
                {
                    endExclusive = i;
                    break;
                }
            }
        }

        // Remove all blocks outside the desired range [selectedIndex+1, endExclusive)
        // Iterate from the end to preserve indices while removing.
        for (var i = document.Count - 1; i >= 0; i--)
        {
            var isWithinRange = i > selectedIndex && i < endExclusive;
            if (!isWithinRange)
            {
                document.RemoveAt(i);
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

    private string GetHeadingText(HeadingBlock headingBlock)
    {
        var sb = new StringBuilder();
        var current = headingBlock.Inline?.FirstChild;
        
        while (current != null)
        {
            sb.Append(GetInlineText(current));
            current = current.NextSibling;
        }
        
        return sb.ToString();
    }

    private string GetInlineText(Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literalInline:
                return literalInline.Content.ToString();
            
            case HtmlEntityInline htmlEntityInline:
                // If the entity couldn't be transcoded, fall back to the original inline string
                {
                    var transcoded = htmlEntityInline.Transcoded.ToString();
                    return transcoded ?? inline.ToString();
                }
            
            case EmphasisInline emphasisInline:
                // Handle *emphasis* or **bold** in headings by getting text from children
                var sb = new StringBuilder();
                var child = emphasisInline.FirstChild;
                while (child != null)
                {
                    sb.Append(GetInlineText(child));
                    child = child.NextSibling;
                }
                return sb.ToString();
            
            case CodeInline codeInline:
                // Handle `code` in headings
                return codeInline.Content;
            
            default:
                // For any other inline types, return empty string to avoid errors
                return string.Empty;
        }
    }
}