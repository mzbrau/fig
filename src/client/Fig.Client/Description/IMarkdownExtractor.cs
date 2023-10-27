namespace Fig.Client.Description;

internal interface IMarkdownExtractor
{
    string ExtractSection(string markdown, string desiredHeading);
}