using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.Capabilities;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Capabilities;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigCapabilityProviderTests
{
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private Mock<ILogger> _loggerMock = null!;
    private HttpClient _httpClient = null!;

    [SetUp]
    public void Setup()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger>();
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
    public async Task FetchAsync_OnSuccess_PopulatesFeaturesAndMarksFetched()
    {
        // Arrange
        var contract = new FigCapabilitiesDataContract("1.0.0", new[] { "deferredDescriptionRegistration", "requestCompression" });
        SetupHttpResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(contract, JsonSettings.FigDefault));
        var provider = CreateProvider();

        // Act
        await provider.FetchAsync();

        // Assert
        Assert.That(provider.Supports("deferredDescriptionRegistration"), Is.True);
        Assert.That(provider.Supports("requestCompression"), Is.True);
        Assert.That(provider.Supports("unknownFeature"), Is.False);
    }

    [Test]
    public async Task FetchAsync_OnSuccess_IsCaseInsensitive()
    {
        // Arrange
        var contract = new FigCapabilitiesDataContract("1.0.0", new[] { "RequestCompression" });
        SetupHttpResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(contract, JsonSettings.FigDefault));
        var provider = CreateProvider();

        // Act
        await provider.FetchAsync();

        // Assert
        Assert.That(provider.Supports("requestcompression"), Is.True);
        Assert.That(provider.Supports("REQUESTCOMPRESSION"), Is.True);
    }

    [Test]
    public async Task FetchAsync_OnSuccess_DoesNotFetchAgainOnSecondCall()
    {
        // Arrange
        var contract = new FigCapabilitiesDataContract("1.0.0", new[] { "deferredDescriptionRegistration" });
        SetupHttpResponse(HttpStatusCode.OK, JsonConvert.SerializeObject(contract, JsonSettings.FigDefault));
        var provider = CreateProvider();

        // Act
        await provider.FetchAsync();
        await provider.FetchAsync();

        // Assert — only one HTTP call should have been made
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task FetchAsync_WithForceTrue_FetchesAgainEvenIfAlreadyFetched()
    {
        // Arrange — first call returns one feature set, second call returns a different one
        var firstContract = new FigCapabilitiesDataContract("1.0.0", new[] { "deferredDescriptionRegistration" });
        var secondContract = new FigCapabilitiesDataContract("1.0.0", new[] { "requestCompression" });
        var firstJson = JsonConvert.SerializeObject(firstContract, JsonSettings.FigDefault);
        var secondJson = JsonConvert.SerializeObject(secondContract, JsonSettings.FigDefault);

        var callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                var json = callCount == 1 ? firstJson : secondJson;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json, Encoding.UTF8, "application/json")
                };
            });

        var provider = CreateProvider();
        await provider.FetchAsync();
        Assert.That(provider.Supports("deferredDescriptionRegistration"), Is.True);
        Assert.That(provider.Supports("requestCompression"), Is.False);

        // Act
        await provider.FetchAsync(force: true);

        // Assert — two HTTP calls were made and second response replaced the first
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(2),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
        Assert.That(provider.Supports("requestCompression"), Is.True);
        Assert.That(provider.Supports("deferredDescriptionRegistration"), Is.False);
    }

    [Test]
    public async Task FetchAsync_On404_SetsEmptyFeaturesAndMarksFetched()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.NotFound, string.Empty);
        var provider = CreateProvider();

        // Act
        await provider.FetchAsync();

        // Assert — no features, but does not retry
        Assert.That(provider.Supports("deferredDescriptionRegistration"), Is.False);

        // Second call should not trigger another HTTP request
        await provider.FetchAsync();
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.IsAny<HttpRequestMessage>(),
            ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task FetchAsync_OnTransientNonSuccessStatus_DoesNotMarkFetched_AllowsRetry()
    {
        // Arrange — first call returns 503, second call returns 200 with features
        var contract = new FigCapabilitiesDataContract("1.0.0", new[] { "requestCompression" });
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(contract, JsonSettings.FigDefault), Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : successResponse;
            });

        var provider = CreateProvider();

        // Act — first fetch fails transiently
        await provider.FetchAsync();
        Assert.That(provider.Supports("requestCompression"), Is.False);

        // Second fetch should retry and succeed
        await provider.FetchAsync();
        Assert.That(provider.Supports("requestCompression"), Is.True);
    }

    [Test]
    public async Task FetchAsync_OnNetworkException_DoesNotMarkFetched_AllowsRetry()
    {
        // Arrange — first call throws, second call succeeds
        var contract = new FigCapabilitiesDataContract("1.0.0", new[] { "deferredDescriptionRegistration" });
        var successResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(contract, JsonSettings.FigDefault), Encoding.UTF8, "application/json")
        };

        var callCount = 0;
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount == 1)
                    throw new HttpRequestException("Connection refused");
                return successResponse;
            });

        var provider = CreateProvider();

        // Act — first fetch throws
        await provider.FetchAsync();
        Assert.That(provider.Supports("deferredDescriptionRegistration"), Is.False);

        // Second fetch should retry and succeed
        await provider.FetchAsync();
        Assert.That(provider.Supports("deferredDescriptionRegistration"), Is.True);
    }

    [Test]
    public void Supports_BeforeFetch_ReturnsFalse()
    {
        // Arrange
        SetupHttpResponse(HttpStatusCode.OK, "{}");
        var provider = CreateProvider();

        // Act & Assert — capabilities not yet fetched
        Assert.That(provider.Supports("deferredDescriptionRegistration"), Is.False);
    }

    private FigCapabilityProvider CreateProvider()
        => new FigCapabilityProvider(_httpClient, _loggerMock.Object);

    private void SetupHttpResponse(HttpStatusCode statusCode, string content)
    {
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() => new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            });
    }
}
