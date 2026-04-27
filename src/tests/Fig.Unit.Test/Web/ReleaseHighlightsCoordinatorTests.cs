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
    private readonly Guid _adminUserId = Guid.NewGuid();
    private Mock<IAccountService> _accountService = null!;
    private Mock<IHttpService> _httpService = null!;
    private Mock<ILocalStorageService> _localStorageService = null!;
    private Mock<IVersionHelper> _versionHelper = null!;
    private ReleaseHighlightsCoordinator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accountService = new Mock<IAccountService>();
        _httpService = new Mock<IHttpService>();
        _localStorageService = new Mock<ILocalStorageService>();
        _versionHelper = new Mock<IVersionHelper>();

        _accountService.SetupGet(x => x.AuthenticatedUser).Returns(new AuthenticatedUserModel
        {
            Id = _adminUserId,
            Role = Role.Administrator,
            Username = "admin"
        });
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.6.0.0");
        _localStorageService.Setup(x => x.GetItem<ReleaseHighlightsCacheState>(It.IsAny<string>()))
            .ReturnsAsync((ReleaseHighlightsCacheState?)null);
        _localStorageService.Setup(x => x.SetItem(It.IsAny<string>(), It.IsAny<ReleaseHighlightsCacheState>()))
            .Returns(Task.CompletedTask);
        _httpService.Setup(x => x.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false))
            .ReturnsAsync(new ReleaseHighlightProgressDataContract(new List<ReleaseHighlightViewDataContract>()));

        _sut = new ReleaseHighlightsCoordinator(
            _accountService.Object,
            new ReleaseHighlightsCatalog(),
            _httpService.Object,
            _localStorageService.Object,
            _versionHelper.Object);
    }

    [Test]
    public async Task ShallShowAllSeeded3XHighlightsOnInitial36Launch()
    {
        var result = await _sut.GetAutoOpenDialog();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items.Count, Is.EqualTo(19));
        Assert.That(result.StartIndex, Is.EqualTo(0));
        Assert.That(result.Items.Select(x => x.ReleaseVersion).Distinct(),
            Is.EquivalentTo(new[] { "3.0", "3.1", "3.2", "3.3", "3.4", "3.5" }));
    }

    [Test]
    public async Task ShallResumeAtFirstUnseenHighlightAcrossPendingVersions()
    {
        _httpService.Setup(x => x.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false))
            .ReturnsAsync(new ReleaseHighlightProgressDataContract(new List<ReleaseHighlightViewDataContract>
            {
                new("3.1", "aspire-integration", DateTime.UtcNow),
                new("3.2", "data-grid-enter-save", DateTime.UtcNow),
                new("3.3", "client-registration-history", DateTime.UtcNow),
                new("3.3", "setting-compare", DateTime.UtcNow),
                new("3.4", "mcp-server", DateTime.UtcNow)
            }));

        var result = await _sut.GetAutoOpenDialog();

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items.Count, Is.EqualTo(15));
        Assert.That(result.Items[result.StartIndex].FeatureKey, Is.EqualTo("provider-defined-lookup-tables"));
    }

    [Test]
    public async Task ShallShortCircuitWhenCacheShowsAllCurrentVersionsComplete()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.5.1.0");
        _localStorageService.Setup(x => x.GetItem<ReleaseHighlightsCacheState>(It.IsAny<string>()))
            .ReturnsAsync(new ReleaseHighlightsCacheState
            {
                UserId = _adminUserId,
                CompletedVersions = new List<string> { "3.0", "3.1", "3.2", "3.3", "3.4", "3.5" }
            });

        var result = await _sut.GetAutoOpenDialog();

        Assert.That(result, Is.Null);
        _httpService.Verify(x => x.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false), Times.Never);
    }

    [Test]
    public async Task ShallPersistCompletedVersionsWhenLastHighlightIsViewed()
    {
        _versionHelper.Setup(x => x.GetVersion()).Returns("3.1.0.0");
        _httpService.Setup(x => x.Post<ReleaseHighlightViewDataContract>(
                "releasehighlights/viewed",
                It.IsAny<ReleaseHighlightViewedDataContract>()))
            .ReturnsAsync(new ReleaseHighlightViewDataContract("3.1", "aspire-integration", DateTime.UtcNow));

        var manualDialog = await _sut.GetManualRecallDialog();
        Assert.That(manualDialog, Is.Not.Null);
        var recorded = await _sut.RecordViewed(manualDialog!.Items.Single(x => x.FeatureKey == "aspire-integration"));

        Assert.That(recorded, Is.True);
        _localStorageService.Verify(x => x.SetItem(
                It.IsAny<string>(),
                It.Is<ReleaseHighlightsCacheState>(state =>
                    state.UserId == _adminUserId &&
                    state.CompletedVersions.Count == 1 &&
                    state.CompletedVersions[0] == "3.1")),
            Times.AtLeastOnce);
    }

    [Test]
    public async Task ShallExposeReadMoreLinksWhenCatalogProvidesThem()
    {
        var manualDialog = await _sut.GetManualRecallDialog();
        Assert.That(manualDialog, Is.Not.Null);

        var providerLookupItem = manualDialog!.Items.Single(x => x.FeatureKey == "provider-defined-lookup-tables");
        var informationTextItem = manualDialog.Items.Single(x => x.FeatureKey == "information-text");
        var monacoEditorItem = manualDialog.Items.Single(x => x.FeatureKey == "monaco-json-editor");

        Assert.That(providerLookupItem.ReadMoreUrl,
            Is.EqualTo("https://www.figsettings.com/docs/features/provider-defined-lookup-tables"));
        Assert.That(informationTextItem.ReadMoreUrl,
            Is.EqualTo("https://www.figsettings.com/docs/features/settings-management/display-scripts"));
        Assert.That(monacoEditorItem.ReadMoreUrl, Is.Null);
    }
}
