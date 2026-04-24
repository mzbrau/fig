using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Text;
using Fig.Client.Capabilities;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Contracts;
using Fig.Contracts.Settings;
using Fig.Contracts.SettingDefinitions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ApiCommunicationHandlerTests
{
    private Mock<ILogger<ApiCommunicationHandler>> _loggerMock = null!;
    private Mock<IClientSecretProvider> _clientSecretProviderMock = null!;
    private Mock<IFigCapabilityProvider> _capabilityProviderMock = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ApiCommunicationHandler>>();
        _clientSecretProviderMock = new Mock<IClientSecretProvider>();
        _capabilityProviderMock = new Mock<IFigCapabilityProvider>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _clientSecretProviderMock.Setup(x => x.GetSecret(It.IsAny<string>())).ReturnsAsync("test-secret");
        _capabilityProviderMock.Setup(x => x.FetchAsync(It.IsAny<bool>())).Returns(Task.CompletedTask);

        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient?.Dispose();
    }

    [Test]
    public async Task RegisterWithFigApi_LogsPayloadSizeAndSettingCount_AtInformationLevel()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var handler = CreateHandler();
        var settings = CreateSettings(settingCount: 3);

        // Act
        await handler.RegisterWithFigApi(settings);

        // Assert — Information log with payload size and setting count is emitted
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) =>
                    v.ToString()!.Contains("Payload size") &&
                    v.ToString()!.Contains("bytes") &&
                    v.ToString()!.Contains("setting count: 3")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task RegisterWithFigApi_LogsElapsedMs_OnSuccess()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var handler = CreateHandler();
        var settings = CreateSettings(settingCount: 1);

        // Act
        await handler.RegisterWithFigApi(settings);

        // Assert — success message includes elapsed ms
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Successfully registered settings") && v.ToString()!.Contains("ms")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task RegisterWithFigApi_WithZeroSettings_StillLogsPayload()
    {
        // Arrange
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var handler = CreateHandler();
        var settings = CreateSettings(settingCount: 0);

        // Act
        await handler.RegisterWithFigApi(settings);

        // Assert — payload log always fires, even with 0 settings
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Payload size")),
                It.IsAny<Exception?>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Test]
    public async Task RegisterWithFigApi_WhenDeferredDescriptionSupported_PostPayloadOmitsDescription()
    {
        // Arrange
        _capabilityProviderMock.Setup(x => x.Supports("deferredDescriptionRegistration")).Returns(true);

        string? capturedPostBody = null;
        var putSignal = new SemaphoreSlim(0, 1);
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                if (req.Method == HttpMethod.Post)
                    capturedPostBody = req.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
                if (req.Method == HttpMethod.Put)
                    putSignal.Release();
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var handler = CreateHandler();
        var settings = CreateSettings(settingCount: 2);

        // Act
        await handler.RegisterWithFigApi(settings);
        var putIssued = await putSignal.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert — POST payload must not contain the description field value
        Assert.That(putIssued, Is.True, "Deferred description PUT should be issued within 5 seconds");
        Assert.That(capturedPostBody, Is.Not.Null);
        Assert.That(capturedPostBody, Does.Not.Contain("A test client"));
    }

    [Test]
    public async Task RegisterWithFigApi_WhenDeferredDescriptionSupported_SendsPutDescriptionRequest()
    {
        // Arrange
        _capabilityProviderMock.Setup(x => x.Supports("deferredDescriptionRegistration")).Returns(true);

        var putRequests = new List<string>();
        var putSignal = new SemaphoreSlim(0, 1);
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                if (req.Method == HttpMethod.Put)
                {
                    putRequests.Add(req.RequestUri!.ToString());
                    putSignal.Release();
                }
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var handler = CreateHandler();
        var settings = CreateSettings(settingCount: 2);

        // Act
        await handler.RegisterWithFigApi(settings);
        var putIssued = await putSignal.WaitAsync(TimeSpan.FromSeconds(5));

        // Assert — a PUT to the description endpoint should have been issued
        Assert.That(putIssued, Is.True, "Deferred description PUT should be issued within 5 seconds");
        Assert.That(putRequests, Has.Count.EqualTo(1));
        Assert.That(putRequests[0], Does.Contain("/clients/TestClient/description"));
    }

    [Test]
    public async Task RegisterWithFigApi_WhenCompressionSupportedAndPayloadExceedsThreshold_SendsGzipCompressedContent()
    {
        // Arrange — enable compression capability and create a large payload (> 4096 bytes)
        _capabilityProviderMock.Setup(x => x.Supports("requestCompression")).Returns(true);

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                capturedRequest = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var handler = CreateHandler();
        // 30 settings each with a 200-char description ensures the serialized payload exceeds the 4096-byte threshold
        var settings = CreateSettings(settingCount: 30, description: new string('x', 200));

        // Act
        await handler.RegisterWithFigApi(settings);

        // Assert — Content-Encoding: gzip must be present
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Content!.Headers.ContentEncoding, Does.Contain("gzip"));

        // Assert — decompressing the body yields valid JSON containing the original content
        var compressedBytes = await capturedRequest.Content.ReadAsByteArrayAsync();
        await using var ms = new MemoryStream(compressedBytes);
        await using var gz = new GZipStream(ms, CompressionMode.Decompress);
        using var reader = new StreamReader(gz, Encoding.UTF8);
        var decompressedJson = await reader.ReadToEndAsync();
        Assert.That(decompressedJson, Does.Contain("TestClient"));
        Assert.That(decompressedJson, Does.Contain("Setting0"));
    }

    [Test]
    public async Task RegisterWithFigApi_WhenCompressionSupportedButPayloadBelowThreshold_SendsUncompressedContent()
    {
        // Arrange — enable compression capability but keep the payload small (< 4096 bytes)
        _capabilityProviderMock.Setup(x => x.Supports("requestCompression")).Returns(true);

        HttpRequestMessage? capturedRequest = null;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken _) =>
            {
                capturedRequest = req;
                return new HttpResponseMessage(HttpStatusCode.OK);
            });

        var handler = CreateHandler();
        var settings = CreateSettings(settingCount: 2); // Small payload, well under 4096 bytes

        // Act
        await handler.RegisterWithFigApi(settings);

        // Assert — no Content-Encoding header (plain JSON, no compression)
        Assert.That(capturedRequest, Is.Not.Null);
        Assert.That(capturedRequest!.Content!.Headers.ContentEncoding, Is.Empty);

        // Assert — body is readable as plain JSON
        var json = await capturedRequest.Content.ReadAsStringAsync();
        Assert.That(json, Does.Contain("TestClient"));
    }

    private ApiCommunicationHandler CreateHandler(string clientName = "TestClient")
    {
        return new ApiCommunicationHandler(
            clientName,
            null,
            _httpClient,
            _loggerMock.Object,
            _clientSecretProviderMock.Object,
            _capabilityProviderMock.Object);
    }

    private static SettingsClientDefinitionDataContract CreateSettings(int settingCount = 2, string description = "A test setting")
    {
        var settings = new List<SettingDefinitionDataContract>();
        for (var i = 0; i < settingCount; i++)
        {
            settings.Add(new SettingDefinitionDataContract($"Setting{i}", description));
        }

        return new SettingsClientDefinitionDataContract(
            name: "TestClient",
            description: "A test client",
            instance: null,
            hasDisplayScripts: false,
            settings: settings,
            clientSettingOverrides: Array.Empty<SettingDataContract>());
    }
}
