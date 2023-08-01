namespace Fig.Client.Description;

public interface IMarkdownExtractor
{
    string ExtractSection(string markdown, string desiredHeading);
}