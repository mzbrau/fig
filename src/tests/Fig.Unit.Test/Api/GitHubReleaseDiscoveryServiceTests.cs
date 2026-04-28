using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fig.Api;
using Fig.Api.Services;
using Fig.Common;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class GitHubReleaseDiscoveryServiceTests
{
    private Mock<IHttpClientFactory> _httpClientFactory = null!;
    private Mock<IOptionsMonitor<ApiSettings>> _apiSettings = null!;
    private Mock<IVersionHelper> _versionHelper = null!;
    private Mock<ILogger<GitHubReleaseDiscoveryService>> _logger = null!;
    private Mock<HttpMessageHandler> _httpMessageHandler = null!;
    private HttpClient _httpClient = null!;
    private IMemoryCache _memoryCache = null!;
    private GitHubReleaseDiscoveryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpClientFactory = new Mock<IHttpClientFactory>();
        _apiSettings = new Mock<IOptionsMonitor<ApiSettings>>();
        _versionHelper = new Mock<IVersionHelper>();
        _logger = new Mock<ILogger<GitHubReleaseDiscoveryService>>();
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandler.Object);
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _apiSettings.SetupGet(x => x.CurrentValue).Returns(new ApiSettings
        {
            DbConnectionString = "Data Source=fig.db;Version=3;New=True"
        });
        _httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);
        _sut = new GitHubReleaseDiscoveryService(_apiSettings.Object, _httpClientFactory.Object, _memoryCache, _versionHelper.Object, _logger.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _httpClient.Dispose();
        _memoryCache.Dispose();
    }

    [Test]
    public async Task ShallReturnNewestAvailableReleaseWhenGitHubContainsNewerVersion()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.0.0");
        SetupResponse("""
            <html>
              <body>
                <a href="/mzbrau/fig/releases/tag/v3.5.1">v3.5.1</a>
                <a href="/mzbrau/fig/releases/tag/v3.4.3">v3.4.3</a>
                <a href="/mzbrau/fig/releases/tag/v3.5.0">v3.5.0</a>
              </body>
            </html>
            """);

        var result = await _sut.GetNewestAvailableReleaseHighlight();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ReleaseVersion, Is.EqualTo("3.5.1"));
        Assert.That(result.FeatureKey, Is.EqualTo("new-release-available"));
        Assert.That(result.Title, Is.EqualTo("Fig v3.5.1 is available"));
        Assert.That(result.Description, Does.Contain("currently running Fig v3.5.0.0"));
        Assert.That(result.ImagePath, Is.EqualTo("images/release-highlights/shared/new-release.png"));
        Assert.That(result.ReadMoreUrl, Is.EqualTo("https://github.com/mzbrau/fig/releases/tag/v3.5.1"));
        Assert.That(result.MarkViewedOnDisplay, Is.False);
    }

    [Test]
    public async Task ShallReturnNullWhenCurrentVersionAlreadyMatchesNewestRelease()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.1.0");
        SetupResponse("""
            <html>
              <body>
                <a href="/mzbrau/fig/releases/tag/v3.5.1">v3.5.1</a>
                <a href="/mzbrau/fig/releases/tag/v3.5.0">v3.5.0</a>
              </body>
            </html>
            """);

        var result = await _sut.GetNewestAvailableReleaseHighlight();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ShallReturnNullWhenGitHubRequestFails()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.0.0");
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        var result = await _sut.GetNewestAvailableReleaseHighlight();

        Assert.That(result, Is.Null);
    }

    private void SetupResponse(string html)
    {
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html)
            });
    }
}
