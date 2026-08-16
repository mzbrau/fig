using System.Net;
using System.Net.Http;
using System.Text;
using Fig.Api.Assistant;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using Moq;
using Moq.Protected;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class OpenAiCompatibleLlmClientTests
{
    private Mock<IHttpClientFactory> _httpClientFactory = null!;
    private Mock<IConfigurationRepository> _configurationRepository = null!;
    private Mock<IEncryptionService> _encryptionService = null!;
    private Mock<HttpMessageHandler> _httpMessageHandler = null!;
    private OpenAiCompatibleLlmClient _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _configurationRepository = new Mock<IConfigurationRepository>();
        _encryptionService = new Mock<IEncryptionService>();
        _httpMessageHandler = new Mock<HttpMessageHandler>();

        _httpClientFactory
            .Setup(f => f.CreateClient("FigAssistantLlm"))
            .Returns(() => new HttpClient(_httpMessageHandler.Object));

        _configurationRepository.Setup(r => r.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity
        {
            FigAssistantEndpoint = "https://llm.example/",
            FigAssistantModel = "test-model",
            FigAssistantAccessTokenEncrypted = "encrypted-token"
        });
        _encryptionService
            .Setup(e => e.Decrypt("encrypted-token", It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns("plain-token");

        _sut = new OpenAiCompatibleLlmClient(
            _httpClientFactory.Object,
            _configurationRepository.Object,
            _encryptionService.Object);
    }

    [Test]
    public async Task StreamChatAsync_YieldsTextToolCallsAndFinishReason()
    {
        HttpRequestMessage? captured = null;
        string? capturedBody = null;
        SetupSseResponse(
            """
            data: {"choices":[{"delta":{"content":"Hello"}}]}

            ignore me
            data: not-json

            data: {"choices":[{"delta":{"tool_calls":[{"index":0,"id":"call_1","function":{"name":"list_clients","arguments":"{\"x\":1}"}}]}}]}

            data: {"choices":[{"delta":{},"finish_reason":"tool_calls"}]}

            data: [DONE]
            """,
            async request =>
            {
                captured = request;
                capturedBody = request.Content is null
                    ? null
                    : await request.Content.ReadAsStringAsync();
            });

        var tool = CreateTool("list_clients", "List clients", """{"type":"object","properties":{}}""");
        var chunks = new List<LlmStreamChunk>();
        await foreach (var chunk in _sut.StreamChatAsync(
                           [new JObject { ["role"] = "user", ["content"] = "hi" }],
                           [tool],
                           CancellationToken.None,
                           temperature: 0.2))
        {
            chunks.Add(chunk);
        }

        Assert.That(chunks, Has.Count.EqualTo(3));
        Assert.That(chunks[0].Text, Is.EqualTo("Hello"));
        Assert.That(chunks[1].ToolCallId, Is.EqualTo("call_1"));
        Assert.That(chunks[1].ToolName, Is.EqualTo("list_clients"));
        Assert.That(chunks[1].ToolArguments, Is.EqualTo("""{"x":1}"""));
        Assert.That(chunks[2].FinishReason, Is.EqualTo("tool_calls"));

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.RequestUri!.ToString(), Is.EqualTo("https://llm.example/chat/completions"));
        Assert.That(captured.Headers.Authorization!.Parameter, Is.EqualTo("plain-token"));
        Assert.That(capturedBody, Is.Not.Null);
        var json = JObject.Parse(capturedBody!);
        Assert.That(json["model"]!.Value<string>(), Is.EqualTo("test-model"));
        Assert.That(json["stream"]!.Value<bool>(), Is.True);
        Assert.That(json["temperature"]!.Value<double>(), Is.EqualTo(0.2));
        Assert.That(json["tool_choice"]!.Value<string>(), Is.EqualTo("auto"));
        Assert.That(json["tools"]![0]!["function"]!["name"]!.Value<string>(), Is.EqualTo("list_clients"));
    }

    [Test]
    public void StreamChatAsync_WhenTokenMissing_Throws()
    {
        _encryptionService
            .Setup(e => e.Decrypt("encrypted-token", It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((string?)null);

        Assert.That(
            async () =>
            {
                await foreach (var _ in _sut.StreamChatAsync([], [], CancellationToken.None))
                {
                }
            },
            Throws.InvalidOperationException.With.Message.Contain("access token is not configured"));
    }

    [Test]
    public void StreamChatAsync_WhenDecryptReturnsCiphertext_Throws()
    {
        _encryptionService
            .Setup(e => e.Decrypt("encrypted-token", It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns("encrypted-token");

        Assert.That(
            async () =>
            {
                await foreach (var _ in _sut.StreamChatAsync([], [], CancellationToken.None))
                {
                }
            },
            Throws.InvalidOperationException.With.Message.Contain("access token is not configured"));
    }

    [Test]
    public void StreamChatAsync_WhenHttpFails_TruncatesErrorBody()
    {
        var longError = new string('e', 600);
        SetupHttpResponse(HttpStatusCode.BadRequest, longError);

        Assert.That(
            async () =>
            {
                await foreach (var _ in _sut.StreamChatAsync([], [], CancellationToken.None))
                {
                }
            },
            Throws.TypeOf<HttpRequestException>()
                .With.Message.Contain("400")
                .And.Message.Contain("...")
                .And.Message.Length.LessThan(600));
    }

    [Test]
    public void ToToolDefinition_BuildsOpenAiFunctionShape()
    {
        var tool = CreateTool(
            "get_events",
            "Query events",
            """{"type":"object","properties":{"startTime":{"type":"string"}},"required":["startTime"]}""");

        var definition = OpenAiCompatibleLlmClient.ToToolDefinition(tool);

        Assert.That(definition["type"]!.Value<string>(), Is.EqualTo("function"));
        Assert.That(definition["function"]!["name"]!.Value<string>(), Is.EqualTo("get_events"));
        Assert.That(definition["function"]!["description"]!.Value<string>(), Is.EqualTo("Query events"));
        Assert.That(definition["function"]!["parameters"]!["required"]![0]!.Value<string>(), Is.EqualTo("startTime"));
    }

    [TestCase("short", 10, "short")]
    [TestCase("abcdefghij", 10, "abcdefghij")]
    [TestCase("abcdefghijk", 10, "abcdefghij...")]
    public void Truncate_RespectsMaximum(string value, int maximum, string expected)
    {
        Assert.That(OpenAiCompatibleLlmClient.Truncate(value, maximum), Is.EqualTo(expected));
    }

    private void SetupSseResponse(string sseBody, Func<HttpRequestMessage, Task>? capture = null)
    {
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Returns(async (HttpRequestMessage request, CancellationToken _) =>
            {
                if (capture is not null)
                    await capture(request);
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(sseBody, Encoding.UTF8, "text/event-stream")
                };
            });
    }

    private void SetupHttpResponse(HttpStatusCode statusCode, string body)
    {
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            });
    }

    private static IAssistantTool CreateTool(string name, string description, string schema)
    {
        var tool = new Mock<IAssistantTool>();
        tool.SetupGet(t => t.Name).Returns(name);
        tool.SetupGet(t => t.Description).Returns(description);
        tool.SetupGet(t => t.ParameterJsonSchema).Returns(schema);
        return tool.Object;
    }
}
