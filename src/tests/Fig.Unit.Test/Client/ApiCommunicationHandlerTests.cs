using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Contracts;
using Fig.Client.Startup;
using Fig.Common.NetStandard.IpAddress;
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
    private Mock<IIpAddressResolver> _ipAddressResolverMock = null!;
    private Mock<IClientSecretProvider> _clientSecretProviderMock = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ApiCommunicationHandler>>();
        _ipAddressResolverMock = new Mock<IIpAddressResolver>();
        _clientSecretProviderMock = new Mock<IClientSecretProvider>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _clientSecretProviderMock.Setup(x => x.GetSecret(It.IsAny<string>())).ReturnsAsync("test-secret");
        _ipAddressResolverMock.Setup(x => x.Resolve()).Returns("127.0.0.1");

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

    private ApiCommunicationHandler CreateHandler(string clientName = "TestClient")
    {
        return new ApiCommunicationHandler(
            clientName,
            null,
            _httpClient,
            _loggerMock.Object,
            _ipAddressResolverMock.Object,
            _clientSecretProviderMock.Object,
            new NoOpServiceStartupExtender());
    }

    private static SettingsClientDefinitionDataContract CreateSettings(int settingCount = 2)
    {
        var settings = new List<SettingDefinitionDataContract>();
        for (var i = 0; i < settingCount; i++)
        {
            settings.Add(new SettingDefinitionDataContract($"Setting{i}", "A test setting"));
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
