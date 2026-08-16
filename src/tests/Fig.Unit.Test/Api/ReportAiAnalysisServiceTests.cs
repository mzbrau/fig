using Fig.Api.Assistant;
using Fig.Api.Reports;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ReportAiAnalysisServiceTests
{
    private Mock<IAssistantBackgroundRunner> _backgroundRunner = null!;
    private ReportAiAnalysisService _sut = null!;
    private string? _capturedUserMessage;
    private string? _capturedSystemPrompt;
    private double? _capturedTemperature;

    [SetUp]
    public void SetUp()
    {
        _backgroundRunner = new Mock<IAssistantBackgroundRunner>();
        _capturedUserMessage = null;
        _capturedSystemPrompt = null;
        _capturedTemperature = null;

        _backgroundRunner
            .Setup(r => r.RunAsync(
                "report-ai-analysis",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .Callback<string, string, string, IReadOnlyCollection<IAssistantTool>, CancellationToken, double?>(
                (_, systemPrompt, userMessage, _, _, temperature) =>
                {
                    _capturedSystemPrompt = systemPrompt;
                    _capturedUserMessage = userMessage;
                    _capturedTemperature = temperature;
                })
            .ReturnsAsync(new AssistantBackgroundRunResult
            {
                AssistantText = "  Summary looks healthy.  ",
                ToolCalls = []
            });

        _sut = new ReportAiAnalysisService(
            _backgroundRunner.Object,
            NullLogger<ReportAiAnalysisService>.Instance);
    }

    [Test]
    public async Task AnalyzeAsync_ReturnsTrimmedAssistantText()
    {
        var result = await _sut.AnalyzeAsync("uptime", new { Clients = 2 }, null, CancellationToken.None);

        Assert.That(result, Is.EqualTo("Summary looks healthy."));
        Assert.That(_capturedTemperature, Is.EqualTo(0.2));
        Assert.That(_capturedSystemPrompt, Does.Contain("Fig Assistant writing an AI analysis"));
        Assert.That(_capturedUserMessage, Does.Contain("Report name: uptime"));
        Assert.That(_capturedUserMessage, Does.Contain("Analyze the data from this report"));
        Assert.That(_capturedUserMessage, Does.Contain("\"Clients\":2"));
    }

    [Test]
    public async Task AnalyzeAsync_UsesCustomUserPrompt()
    {
        await _sut.AnalyzeAsync("uptime", new { Clients = 1 }, "  Focus on failures  ", CancellationToken.None);

        Assert.That(_capturedUserMessage, Does.Contain("Focus on failures"));
        Assert.That(_capturedUserMessage, Does.Not.Contain("Analyze the data from this report"));
    }

    [Test]
    public async Task AnalyzeAsync_MasksSecretsAndTruncatesLargeStrings()
    {
        var model = new
        {
            Password = "super-secret",
            Notes = new string('n', 4_500),
            Nested = new { AccessToken = "token-value", Ok = true }
        };

        await _sut.AnalyzeAsync("security", model, "summarize", CancellationToken.None);

        Assert.That(_capturedUserMessage, Does.Contain("\"Password\":\"[REDACTED]\""));
        Assert.That(_capturedUserMessage, Does.Contain("\"AccessToken\":\"[REDACTED]\""));
        Assert.That(_capturedUserMessage, Does.Contain("...[truncated]"));
        Assert.That(_capturedUserMessage, Does.Not.Contain(new string('n', 4_500)));
    }

    [Test]
    public async Task AnalyzeAsync_TruncatesOverallJsonWhenTooLarge()
    {
        var chunks = Enumerable.Range(0, 20)
            .ToDictionary(i => $"Field{i}", _ => new string('x', 3_000));

        await _sut.AnalyzeAsync("large", chunks, null, CancellationToken.None);

        Assert.That(_capturedUserMessage, Does.Contain("...[truncated for AI analysis]"));
        var jsonStart = _capturedUserMessage!.IndexOf("Report data (JSON):", StringComparison.Ordinal);
        Assert.That(jsonStart, Is.GreaterThanOrEqualTo(0));
        var json = _capturedUserMessage[(jsonStart + "Report data (JSON):".Length)..].Trim();
        Assert.That(json.Length, Is.LessThanOrEqualTo(40_000 + "...[truncated for AI analysis]".Length));
    }

    [Test]
    public async Task AnalyzeAsync_WhenAssistantTextBlank_ReturnsNull()
    {
        _backgroundRunner
            .Setup(r => r.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .ReturnsAsync(new AssistantBackgroundRunResult
            {
                AssistantText = "   ",
                ToolCalls = []
            });

        var result = await _sut.AnalyzeAsync("uptime", new { }, null, CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task AnalyzeAsync_WhenRunnerThrows_ReturnsNull()
    {
        _backgroundRunner
            .Setup(r => r.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .ThrowsAsync(new InvalidOperationException("LLM down"));

        var result = await _sut.AnalyzeAsync("uptime", new { }, null, CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void AnalyzeAsync_WhenCancelled_Propagates()
    {
        _backgroundRunner
            .Setup(r => r.RunAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<IReadOnlyCollection<IAssistantTool>>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<double?>()))
            .ThrowsAsync(new OperationCanceledException());

        Assert.That(
            async () => await _sut.AnalyzeAsync("uptime", new { }, null, CancellationToken.None),
            Throws.InstanceOf<OperationCanceledException>());
    }
}
