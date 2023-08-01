namespace Fig.Common.MarkdownUtils.Description;

public interface IMarkdownExtractor
{
    string ExtractSection(string markdown, string desiredHeading);
}