using System.Net;
using System.Net.Http;
using System.Text;
using Fig.Client.Abstractions.Data;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Authentication;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class FigAuthHandlerTests
{
    private IOptions<McpSettings> _options = null!;
    private Mock<ILogger<FigAuthHandler>> _logger = null!;

    [SetUp]
    public void SetUp()
    {
        var settings = new McpSettings
        {
            FigApiBaseUrl = "https://localhost:7281",
            Username = "admin",
            Password = "test-password"
        };
        _options = Options.Create(settings);
        _logger = new Mock<ILogger<FigAuthHandler>>();
    }

    [Test]
    public async Task SendAsync_AuthRequest_ShouldPassThroughWithoutAddingToken()
    {
        var innerHandler = new TestInnerHandler();
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7281/") };

        var response = await client.PostAsync("users/authenticate",
            new StringContent("{}", Encoding.UTF8, "application/json"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(innerHandler.SentRequests, Has.Count.EqualTo(1));
        Assert.That(innerHandler.SentRequests[0].Headers.Authorization, Is.Null,
            "auth requests should pass through without adding a Bearer token");
    }

    [Test]
    public async Task SendAsync_NonAuthRequest_ShouldAuthenticateAndAddBearerToken()
    {
        var token = CreateTestJwt(DateTime.UtcNow.AddHours(1));
        var innerHandler = new TestInnerHandler();
        innerHandler.QueueResponse(CreateAuthResponse(token));
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7281/") };

        var response = await client.GetAsync("clients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        Assert.That(innerHandler.SentRequests, Has.Count.EqualTo(2));

        // First request should be the auth POST
        Assert.That(innerHandler.SentRequests[0].Method, Is.EqualTo(HttpMethod.Post));
        Assert.That(innerHandler.SentRequests[0].RequestUri!.OriginalString, Does.Contain("authenticate"));

        // Second request should have Bearer token
        Assert.That(innerHandler.SentRequests[1].Headers.Authorization, Is.Not.Null);
        Assert.That(innerHandler.SentRequests[1].Headers.Authorization!.Scheme, Is.EqualTo("Bearer"));
        Assert.That(innerHandler.SentRequests[1].Headers.Authorization!.Parameter, Is.EqualTo(token));
    }

    [Test]
    public async Task SendAsync_CachedToken_ShouldReuseTokenWithoutReauthenticating()
    {
        var token = CreateTestJwt(DateTime.UtcNow.AddHours(1));
        var innerHandler = new TestInnerHandler();
        // First request: auth + actual
        innerHandler.QueueResponse(CreateAuthResponse(token));
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK));
        // Second request: only actual (token cached)
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7281/") };

        await client.GetAsync("clients");
        await client.GetAsync("events");

        // Should be 3 total: 1 auth + 2 actual requests (no second auth)
        Assert.That(innerHandler.SentRequests, Has.Count.EqualTo(3));
        Assert.That(innerHandler.SentRequests.Count(r =>
            r.RequestUri!.OriginalString.Contains("authenticate")), Is.EqualTo(1));
    }

    [Test]
    public async Task SendAsync_On401_ShouldRetryRequest()
    {
        var token = CreateTestJwt(DateTime.UtcNow.AddHours(1));
        var freshToken = CreateTestJwt(DateTime.UtcNow.AddHours(2));
        var innerHandler = new TestInnerHandler();
        // Initial auth
        innerHandler.QueueResponse(CreateAuthResponse(token));
        // First attempt returns 401
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.Unauthorized));
        // Re-auth after token invalidation
        innerHandler.QueueResponse(CreateAuthResponse(freshToken));
        // Retry with fresh token
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7281/") };

        var response = await client.GetAsync("clients");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        // 4 requests: auth, original (401), re-auth, retry (200)
        Assert.That(innerHandler.SentRequests, Has.Count.EqualTo(4));
    }

    [Test]
    public async Task SendAsync_WithExpiredToken_ShouldReauthenticateOnNextRequest()
    {
        var expiredToken = CreateTestJwt(DateTime.UtcNow.AddHours(-2));
        var freshToken = CreateTestJwt(DateTime.UtcNow.AddHours(1));
        var innerHandler = new TestInnerHandler();
        // First request: auth (expired token) + actual
        innerHandler.QueueResponse(CreateAuthResponse(expiredToken));
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK));
        // Second request: re-auth (fresh token) + actual
        innerHandler.QueueResponse(CreateAuthResponse(freshToken));
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.OK));

        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7281/") };

        await client.GetAsync("clients");
        await client.GetAsync("events");

        // Should be 4: auth1 + req1 + auth2 + req2
        Assert.That(innerHandler.SentRequests, Has.Count.EqualTo(4));
        Assert.That(innerHandler.SentRequests.Count(r =>
            r.RequestUri!.OriginalString.Contains("authenticate")), Is.EqualTo(2));
    }

    [Test]
    public void SendAsync_AuthFailure_ShouldThrowHttpRequestException()
    {
        var innerHandler = new TestInnerHandler();
        innerHandler.QueueResponse(new HttpResponseMessage(HttpStatusCode.Forbidden)
        {
            Content = new StringContent("Invalid credentials")
        });

        using var handler = CreateHandler(innerHandler);
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost:7281/") };

        var ex = Assert.ThrowsAsync<HttpRequestException>(async () =>
            await client.GetAsync("clients"));
        Assert.That(ex!.Message, Does.Contain("authentication failed"));
    }

    [Test]
    public void Dispose_ShouldNotThrow()
    {
        var innerHandler = new TestInnerHandler();
        var handler = CreateHandler(innerHandler);

        Assert.DoesNotThrow(() => handler.Dispose());
    }

    private FigAuthHandler CreateHandler(HttpMessageHandler innerHandler)
    {
        return new FigAuthHandler(_options, _logger.Object)
        {
            InnerHandler = innerHandler
        };
    }

    private static HttpResponseMessage CreateAuthResponse(string token)
    {
        var response = new AuthenticateResponseDataContract(
            Guid.NewGuid(), "admin", "Admin", "User",
            Role.Administrator, token, false, new List<Classification>());

        var json = JsonConvert.SerializeObject(response, JsonSettings.FigDefault);
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        };
    }

    private static string CreateTestJwt(DateTime expiry)
    {
        var header = Base64UrlEncode("{\"alg\":\"HS256\",\"typ\":\"JWT\"}");
        var exp = new DateTimeOffset(expiry).ToUnixTimeSeconds();
        var payload = Base64UrlEncode($"{{\"exp\":{exp},\"sub\":\"admin\"}}");
        return $"{header}.{payload}.dGVzdC1zaWduYXR1cmU";
    }

    private static string Base64UrlEncode(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        return Convert.ToBase64String(bytes)
            .TrimEnd('=')
            .Replace('+', '-')
            .Replace('/', '_');
    }

    /// <summary>
    /// Test double for the inner HttpMessageHandler that queues responses.
    /// </summary>
    private class TestInnerHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> _responses = new();
        public List<HttpRequestMessage> SentRequests { get; } = new();

        public void QueueResponse(HttpResponseMessage response)
        {
            _responses.Enqueue(response);
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            SentRequests.Add(request);
            if (_responses.Count == 0)
                throw new InvalidOperationException(
                    $"No more queued responses. Request was: {request.Method} {request.RequestUri}");
            return Task.FromResult(_responses.Dequeue());
        }
    }
}
