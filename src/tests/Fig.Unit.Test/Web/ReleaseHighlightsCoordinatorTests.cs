using Fig.Common;
using Fig.Contracts.Authentication;
using Fig.Contracts.ReleaseHighlights;
using Fig.Web.Models.Authentication;
using Fig.Web.ReleaseHighlights;
using Fig.Web.Services;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class ReleaseHighlightsCoordinatorTests
{
    private Mock<IAccountService> _accountService = null!;
    private Mock<IHttpService> _httpService = null!;
    private Mock<IVersionHelper> _versionHelper = null!;
    private ReleaseHighlightsCoordinator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accountService = new Mock<IAccountService>();
        _httpService = new Mock<IHttpService>();
        _versionHelper = new Mock<IVersionHelper>();

        _accountService.SetupGet(x => x.AuthenticatedUser).Returns(new AuthenticatedUserModel
        {
            Id = Guid.NewGuid(),
            Role = Role.Administrator,
            Username = "admin"
        });
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.6.0.0");
        _httpService.Setup(x => x.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false))
            .ReturnsAsync(new ReleaseHighlightProgressDataContract(new List<ReleaseHighlightViewDataContract>()));

        _sut = new ReleaseHighlightsCoordinator(
            _accountService.Object,
            new ReleaseHighlightsCatalog(),
            _httpService.Object,
            _versionHelper.Object);
    }

    [Test]
    public async Task ShallShowAllSeededHighlightsOnInitial36Launch()
    {
        var result = await _sut.GetAutoOpenDialog();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items.Count, Is.EqualTo(14));
        Assert.That(result.StartIndex, Is.EqualTo(0));
        Assert.That(result.Items.Select(x => x.ReleaseVersion).Distinct(),
            Is.EquivalentTo(new[] { "3.0", "3.1", "3.3", "3.4", "3.5", "3.6" }));
    }

    [Test]
    public async Task ShallResumeAtFirstUnseenHighlightWithinIncompleteVersion()
    {
        _httpService.Setup(x => x.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false))
            .ReturnsAsync(new ReleaseHighlightProgressDataContract(new List<ReleaseHighlightViewDataContract>
            {
                new("3.0", "3.0-provider-defined-lookup-tables", DateTime.UtcNow),
                new("3.0", "3.0-validatecount-attribute", DateTime.UtcNow),
                new("3.0", "3.0-custom-predefined-categories", DateTime.UtcNow),
                new("3.0", "3.0-monaco-json-editor", DateTime.UtcNow),
                new("3.0", "3.0-timeline-feature", DateTime.UtcNow),
                new("3.0", "3.0-dependson-attribute", DateTime.UtcNow),
                new("3.0", "3.0-setting-headings", DateTime.UtcNow),
                new("3.1", "3.1-aspire-integration", DateTime.UtcNow),
                new("3.3", "3.3-client-registration-history", DateTime.UtcNow),
                new("3.3", "3.3-setting-compare", DateTime.UtcNow),
                new("3.4", "3.4-group-value-mismatch-detection", DateTime.UtcNow),
                new("3.5", "3.5-custom-groups", DateTime.UtcNow)
            }));

        var result = await _sut.GetAutoOpenDialog();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items.Count, Is.EqualTo(3));
        Assert.That(result.StartIndex, Is.EqualTo(1));
        Assert.That(result.Items[result.StartIndex].FeatureKey, Is.EqualTo("3.5-information-text"));
    }

    [Test]
    public async Task ShallShowDynamicHighlightWhenServerReturnsNewerVersion()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.1.0.0");
        _httpService.Setup(x => x.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false))
            .ReturnsAsync(new ReleaseHighlightProgressDataContract(
                new List<ReleaseHighlightViewDataContract>
                {
                    new("3.0", "3.0-provider-defined-lookup-tables", DateTime.UtcNow),
                    new("3.0", "3.0-validatecount-attribute", DateTime.UtcNow),
                    new("3.0", "3.0-custom-predefined-categories", DateTime.UtcNow),
                    new("3.0", "3.0-monaco-json-editor", DateTime.UtcNow),
                    new("3.0", "3.0-timeline-feature", DateTime.UtcNow),
                    new("3.0", "3.0-dependson-attribute", DateTime.UtcNow),
                    new("3.0", "3.0-setting-headings", DateTime.UtcNow),
                    new("3.1", "3.1-aspire-integration", DateTime.UtcNow)
                },
                new List<ReleaseHighlightCatalogItemDataContract>
                {
                    new(
                        "3.5.1",
                        "new-release-available",
                        "Fig v3.5.1 is available",
                        "A newer Fig release is available.",
                        "images/release-highlights/shared/new-release.png",
                        int.MaxValue,
                        "https://github.com/mzbrau/fig/releases/tag/v3.5.1",
                        markViewedOnDisplay: false)
                }));

        var result = await _sut.GetAutoOpenDialog();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items.Count, Is.EqualTo(1));
        Assert.That(result.Items.Single().FeatureKey, Is.EqualTo("new-release-available"));
        _httpService.Verify(x => x.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false), Times.Once);
    }

    [Test]
    public async Task ShallRecordViewedWhenLastStaticHighlightIsViewed()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.1.0.0");
        _httpService.Setup(x => x.Post<ReleaseHighlightViewDataContract>(
                "releasehighlights/viewed",
                It.IsAny<ReleaseHighlightViewedDataContract>()))
            .ReturnsAsync(new ReleaseHighlightViewDataContract("3.1", "3.1-aspire-integration", DateTime.UtcNow));

        var manualDialog = await _sut.GetManualRecallDialog();
        Assert.That(manualDialog, Is.Not.Null);
        var recorded = await _sut.RecordViewed(manualDialog!.Items.Single(x => x.FeatureKey == "3.1-aspire-integration"));

        Assert.That(recorded, Is.True);
    }

    [Test]
    public async Task ShallExposeReadMoreLinksWhenCatalogProvidesThem()
    {
        var manualDialog = await _sut.GetManualRecallDialog();
        Assert.That(manualDialog, Is.Not.Null);

        var providerLookupItem = manualDialog!.Items.Single(x => x.FeatureKey == "3.0-provider-defined-lookup-tables");
        var informationTextItem = manualDialog.Items.Single(x => x.FeatureKey == "3.5-information-text");
        var migrateFromItem = manualDialog.Items.Single(x => x.FeatureKey == "3.6-migrate-from-attribute");
        var monacoEditorItem = manualDialog.Items.Single(x => x.FeatureKey == "3.0-monaco-json-editor");

        Assert.That(providerLookupItem.ReadMoreUrl,
            Is.EqualTo("https://www.figsettings.com/docs/features/provider-defined-lookup-tables"));
        Assert.That(informationTextItem.ReadMoreUrl,
            Is.EqualTo("https://www.figsettings.com/docs/features/settings-management/display-scripts"));
        Assert.That(migrateFromItem.ReadMoreUrl,
            Is.EqualTo("https://www.figsettings.com/docs/features/settings-management/migrate-from"));
        Assert.That(monacoEditorItem.ReadMoreUrl, Is.Null);
    }

    [Test]
    public async Task ShallAppendDynamicReleaseItemsAfterSeededCatalog()
    {
        _httpService.Setup(x => x.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false))
            .ReturnsAsync(new ReleaseHighlightProgressDataContract(
                new List<ReleaseHighlightViewDataContract>(),
                new List<ReleaseHighlightCatalogItemDataContract>
                {
                    new(
                        "3.6.0",
                        "new-release-available",
                        "Fig v3.6.0 is available",
                        "A newer Fig release is available.",
                        "images/release-highlights/shared/new-release.png",
                        int.MaxValue,
                        "https://github.com/mzbrau/fig/releases/tag/v3.6.0",
                        markViewedOnDisplay: false)
                }));

        var manualDialog = await _sut.GetManualRecallDialog();

        Assert.That(manualDialog, Is.Not.Null);
        var lastItem = manualDialog!.Items.Last();
        Assert.That(lastItem.FeatureKey, Is.EqualTo("new-release-available"));
        Assert.That(lastItem.ReleaseVersion, Is.EqualTo("3.6.0"));
        Assert.That(lastItem.MarkViewedOnDisplay, Is.False);
        Assert.That(lastItem.ImagePath, Is.EqualTo("images/release-highlights/shared/new-release.png"));
        Assert.That(lastItem.ReadMoreUrl, Is.EqualTo("https://github.com/mzbrau/fig/releases/tag/v3.6.0"));
    }
}
