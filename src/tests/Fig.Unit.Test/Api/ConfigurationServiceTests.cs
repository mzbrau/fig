using System.Net;
using System.Net.Http;
using System.Text;
using Fig.Api;
using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Secrets;
using Fig.Api.Services;
using Fig.Contracts.Configuration;
using Fig.Datalayer.BusinessEntities;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ConfigurationServiceTests
{
    private Mock<IConfigurationRepository> _configurationRepository = null!;
    private Mock<IEventLogRepository> _eventLogRepository = null!;
    private Mock<IEventLogFactory> _eventLogFactory = null!;
    private Mock<IFigConfigurationConverter> _converter = null!;
    private Mock<ISecretStore> _secretStore = null!;
    private Mock<IEncryptionService> _encryptionService = null!;
    private Mock<IHttpClientFactory> _httpClientFactory = null!;
    private Mock<HttpMessageHandler> _httpMessageHandler = null!;
    private ConfigurationService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _configurationRepository = new Mock<IConfigurationRepository>();
        _eventLogRepository = new Mock<IEventLogRepository>();
        _eventLogFactory = new Mock<IEventLogFactory>();
        _converter = new Mock<IFigConfigurationConverter>();
        _secretStore = new Mock<ISecretStore>();
        _encryptionService = new Mock<IEncryptionService>();
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _httpMessageHandler = new Mock<HttpMessageHandler>();

        _httpClientFactory
            .Setup(f => f.CreateClient("FigAssistant"))
            .Returns(() => new HttpClient(_httpMessageHandler.Object));

        _sut = new ConfigurationService(
            _configurationRepository.Object,
            _eventLogRepository.Object,
            _eventLogFactory.Object,
            _converter.Object,
            _secretStore.Object,
            _encryptionService.Object,
            _httpClientFactory.Object);
    }

    [Test]
    public async Task TestAzureKeyVault_DelegatesToSecretStore()
    {
        _secretStore
            .Setup(s => s.PerformTest())
            .ReturnsAsync(new SecretStoreTestResultDataContract(true, "Key vault ok"));

        var result = await _sut.TestAzureKeyVault();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Is.EqualTo("Key vault ok"));
        _secretStore.Verify(s => s.PerformTest(), Times.Once);
    }

    [Test]
    public async Task TestFigAssistant_WhenEndpointMissing_ReturnsFailure()
    {
        SetupAssistantConfiguration(endpoint: null, model: "gpt", encryptedToken: "enc");

        var result = await _sut.TestFigAssistant();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("endpoint is not configured"));
    }

    [Test]
    public async Task TestFigAssistant_WhenModelMissing_ReturnsFailure()
    {
        SetupAssistantConfiguration(endpoint: "https://llm.example", model: " ", encryptedToken: "enc");

        var result = await _sut.TestFigAssistant();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("model is not configured"));
    }

    [Test]
    public async Task TestFigAssistant_WhenDecryptFails_ReturnsFailure()
    {
        SetupAssistantConfiguration(endpoint: "https://llm.example", model: "gpt", encryptedToken: "enc");
        _encryptionService
            .Setup(e => e.Decrypt("enc", It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns((string?)null);

        var result = await _sut.TestFigAssistant();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("access token is not configured"));
    }

    [Test]
    public async Task TestFigAssistant_WhenDecryptReturnsCiphertext_ReturnsFailure()
    {
        SetupAssistantConfiguration(endpoint: "https://llm.example", model: "gpt", encryptedToken: "enc");
        _encryptionService
            .Setup(e => e.Decrypt("enc", It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns("enc");

        var result = await _sut.TestFigAssistant();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("access token is not configured"));
    }

    [Test]
    public async Task TestFigAssistant_WhenHttpSucceeds_ReturnsSuccess()
    {
        SetupAssistantConfiguration(endpoint: "https://llm.example/", model: "gpt", encryptedToken: "enc");
        _encryptionService
            .Setup(e => e.Decrypt("enc", It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns("plain-token");
        SetupHttpResponse(HttpStatusCode.OK, "{}");

        var result = await _sut.TestFigAssistant();

        Assert.That(result.Success, Is.True);
        Assert.That(result.Message, Does.Contain("Successfully connected"));
        _httpMessageHandler.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.ToString() == "https://llm.example/models" &&
                r.Headers.Authorization!.Parameter == "plain-token"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task TestFigAssistant_WhenHttpFails_IncludesTruncatedBody()
    {
        SetupAssistantConfiguration(endpoint: "https://llm.example", model: "gpt", encryptedToken: "enc");
        _encryptionService
            .Setup(e => e.Decrypt("enc", It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns("plain-token");
        SetupHttpResponse(HttpStatusCode.Unauthorized, new string('x', 400));

        var result = await _sut.TestFigAssistant();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("401"));
        Assert.That(result.Message, Does.Contain("…"));
        Assert.That(result.Message.Length, Is.LessThan(400));
    }

    [Test]
    public async Task TestFigAssistant_WhenHttpThrows_ReturnsFailure()
    {
        SetupAssistantConfiguration(endpoint: "https://llm.example", model: "gpt", encryptedToken: "enc");
        _encryptionService
            .Setup(e => e.Decrypt("enc", It.IsAny<bool>(), It.IsAny<bool>()))
            .Returns("plain-token");
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("connection refused"));

        var result = await _sut.TestFigAssistant();

        Assert.That(result.Success, Is.False);
        Assert.That(result.Message, Does.Contain("Failed to reach LLM endpoint"));
        Assert.That(result.Message, Does.Contain("connection refused"));
    }

    private void SetupAssistantConfiguration(string? endpoint, string? model, string? encryptedToken)
    {
        _configurationRepository.Setup(r => r.GetConfiguration()).ReturnsAsync(new FigConfigurationBusinessEntity
        {
            FigAssistantEndpoint = endpoint,
            FigAssistantModel = model,
            FigAssistantAccessTokenEncrypted = encryptedToken
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
}
