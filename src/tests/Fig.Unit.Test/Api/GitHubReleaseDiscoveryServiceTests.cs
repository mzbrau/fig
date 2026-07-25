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
    private Mock<IOptionsMonitor<ApiSettings>> _apiSettings = null!;
    private Mock<IVersionHelper> _versionHelper = null!;
    private Mock<ILogger<GitHubReleaseDiscoveryService>> _logger = null!;
    private Mock<HttpMessageHandler> _httpMessageHandler = null!;
    private IMemoryCache _memoryCache = null!;
    private GitHubReleaseDiscoveryService _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _apiSettings = new Mock<IOptionsMonitor<ApiSettings>>();
        _versionHelper = new Mock<IVersionHelper>();
        _logger = new Mock<ILogger<GitHubReleaseDiscoveryService>>();
        _httpMessageHandler = new Mock<HttpMessageHandler>();
        _memoryCache = new MemoryCache(new MemoryCacheOptions());

        _apiSettings.SetupGet(x => x.CurrentValue).Returns(new ApiSettings
        {
            DbConnectionString = "Data Source=fig.db;Version=3;New=True"
        });
        _sut = new GitHubReleaseDiscoveryService(
            _apiSettings.Object,
            _memoryCache,
            _versionHelper.Object,
            _logger.Object,
            _httpMessageHandler.Object);
    }

    [TearDown]
    public void TearDown()
    {
        _memoryCache.Dispose();
    }

    [Test]
    public async Task ShallReturnNullFromCacheWhenCacheIsCold()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.0.0");

        var result = await _sut.GetNewestAvailableReleaseHighlight();

        Assert.That(result, Is.Null);
        _httpMessageHandler.Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Never(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task ShallReturnNewestAvailableReleaseAfterRefreshWhenGitHubContainsNewerVersion()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.0.0");
        SetupResponse("""
            {
              "tag_name": "v3.5.1",
              "html_url": "https://github.com/mzbrau/fig/releases/tag/v3.5.1"
            }
            """);

        await _sut.RefreshAsync();
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
    public async Task ShallReturnNullAfterRefreshWhenCurrentVersionAlreadyMatchesNewestRelease()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.1.0");
        SetupResponse("""
            {
              "tag_name": "v3.5.1",
              "html_url": "https://github.com/mzbrau/fig/releases/tag/v3.5.1"
            }
            """);

        await _sut.RefreshAsync();
        var result = await _sut.GetNewestAvailableReleaseHighlight();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ShallReturnNullAfterRefreshWhenGitHubRequestFails()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.0.0");
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        await _sut.RefreshAsync();
        var result = await _sut.GetNewestAvailableReleaseHighlight();

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ShallNotCallGitHubOnGetAfterCacheIsWarm()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.0.0");
        SetupResponse("""
            {
              "tag_name": "v3.5.1",
              "html_url": "https://github.com/mzbrau/fig/releases/tag/v3.5.1"
            }
            """);

        await _sut.RefreshAsync();
        _httpMessageHandler.Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());

        var result = await _sut.GetNewestAvailableReleaseHighlight();

        Assert.That(result, Is.Not.Null);
        _httpMessageHandler.Protected()
            .Verify<Task<HttpResponseMessage>>(
                "SendAsync",
                Times.Once(),
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>());
    }

    [Test]
    public async Task ShallKeepExistingCachedHighlightWhenRefreshFails()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.0.0");
        SetupResponse("""
            {
              "tag_name": "v3.5.1",
              "html_url": "https://github.com/mzbrau/fig/releases/tag/v3.5.1"
            }
            """);

        await _sut.RefreshAsync();
        var cached = await _sut.GetNewestAvailableReleaseHighlight();
        Assert.That(cached, Is.Not.Null);

        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));

        await _sut.RefreshAsync();
        var result = await _sut.GetNewestAvailableReleaseHighlight();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.ReleaseVersion, Is.EqualTo("3.5.1"));
        Assert.That(result.FeatureKey, Is.EqualTo("new-release-available"));
    }

    private void SetupResponse(string json)
    {
        _httpMessageHandler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json)
            });
    }
}
