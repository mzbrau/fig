using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Services;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Contracts.ReleaseHighlights;
using Fig.Datalayer.BusinessEntities;
using Moq;
using NUnit.Framework;
using ISession = NHibernate.ISession;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ReleaseHighlightsServiceTests
{
    private Mock<IFigReleaseDiscoveryService> _discoveryService = null!;
    private Mock<IReleaseHighlightViewRepository> _viewRepository = null!;
    private Mock<ISession> _session = null!;
    private ReleaseHighlightsService _sut = null!;
    private Guid _userId;

    [SetUp]
    public void SetUp()
    {
        _discoveryService = new Mock<IFigReleaseDiscoveryService>();
        _viewRepository = new Mock<IReleaseHighlightViewRepository>();
        _session = new Mock<ISession>();
        _userId = Guid.NewGuid();

        _sut = new ReleaseHighlightsService(_discoveryService.Object, _viewRepository.Object, _session.Object);
        _sut.SetAuthenticatedUser(new UserDataContract(
            _userId,
            "admin",
            "Admin",
            "User",
            Role.Administrator,
            "*",
            new List<Classification>()));
    }

    [Test]
    public async Task ShallIncludeAvailableHighlightWhenNotYetViewed()
    {
        _viewRepository.Setup(x => x.GetViews(_userId))
            .ReturnsAsync(new List<ReleaseHighlightViewBusinessEntity>());
        _discoveryService.Setup(x => x.GetNewestAvailableReleaseHighlight())
            .ReturnsAsync(CreateAvailableHighlight("3.7.0"));

        var progress = await _sut.GetProgress();

        Assert.That(progress.AvailableHighlights.Count, Is.EqualTo(1));
        Assert.That(progress.AvailableHighlights[0].ReleaseVersion, Is.EqualTo("3.7.0"));
        Assert.That(progress.AvailableHighlights[0].FeatureKey, Is.EqualTo("new-release-available"));
    }

    [Test]
    public async Task ShallOmitAvailableHighlightWhenAlreadyViewed()
    {
        _viewRepository.Setup(x => x.GetViews(_userId))
            .ReturnsAsync(new List<ReleaseHighlightViewBusinessEntity>
            {
                new()
                {
                    UserId = _userId,
                    ReleaseVersion = "3.7.0",
                    FeatureKey = "new-release-available",
                    ViewedAtUtc = DateTime.UtcNow
                }
            });
        _discoveryService.Setup(x => x.GetNewestAvailableReleaseHighlight())
            .ReturnsAsync(CreateAvailableHighlight("3.7.0"));

        var progress = await _sut.GetProgress();

        Assert.That(progress.AvailableHighlights, Is.Empty);
        Assert.That(progress.ViewedHighlights.Count, Is.EqualTo(1));
    }

    [Test]
    public async Task ShallReturnEmptyAvailableHighlightsWhenCacheIsCold()
    {
        _viewRepository.Setup(x => x.GetViews(_userId))
            .ReturnsAsync(new List<ReleaseHighlightViewBusinessEntity>());
        _discoveryService.Setup(x => x.GetNewestAvailableReleaseHighlight())
            .ReturnsAsync((ReleaseHighlightCatalogItemDataContract?)null);

        var progress = await _sut.GetProgress();

        Assert.That(progress.AvailableHighlights, Is.Empty);
        Assert.That(progress.ViewedHighlights, Is.Empty);
    }

    private static ReleaseHighlightCatalogItemDataContract CreateAvailableHighlight(string releaseVersion)
    {
        return new ReleaseHighlightCatalogItemDataContract(
            releaseVersion,
            "new-release-available",
            $"Fig v{releaseVersion} is available",
            "A newer Fig release is available.",
            "images/release-highlights/shared/new-release.png",
            int.MaxValue,
            $"https://github.com/mzbrau/fig/releases/tag/v{releaseVersion}",
            markViewedOnDisplay: false);
    }
}
