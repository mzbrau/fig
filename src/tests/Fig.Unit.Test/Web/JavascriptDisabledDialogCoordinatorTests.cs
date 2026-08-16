using Fig.Contracts.Authentication;
using Fig.Web.Facades;
using Fig.Web.Javascript;
using Fig.Web.Models.Authentication;
using Fig.Web.Services;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class JavascriptDisabledDialogCoordinatorTests
{
    private Mock<IAccountService> _accountService = null!;
    private Mock<IConfigurationFacade> _configurationFacade = null!;
    private Mock<ILocalStorageService> _localStorageService = null!;
    private JavascriptDisabledDialogCoordinator _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _accountService = new Mock<IAccountService>();
        _configurationFacade = new Mock<IConfigurationFacade>();
        _localStorageService = new Mock<ILocalStorageService>();

        _sut = new JavascriptDisabledDialogCoordinator(
            _accountService.Object,
            _configurationFacade.Object,
            _localStorageService.Object);
    }

    [Test]
    public async Task ShallNotOpenForNonAdministrators()
    {
        _accountService.SetupGet(x => x.AuthenticatedUser).Returns(new AuthenticatedUserModel
        {
            Role = Role.User,
            Username = "user"
        });

        Assert.That(await _sut.ShouldAutoOpen(), Is.False);
    }

    [Test]
    public async Task ShallOpenForAdministratorWhenJavascriptDisabledAndNotSuppressed()
    {
        _accountService.SetupGet(x => x.AuthenticatedUser).Returns(new AuthenticatedUserModel
        {
            Id = Guid.NewGuid(),
            Role = Role.Administrator,
            Username = "admin"
        });
        _configurationFacade.SetupGet(x => x.WebFeaturesLoaded).Returns(true);
        _configurationFacade.SetupGet(x => x.AllowDisplayScripts).Returns(false);
        _localStorageService.Setup(x => x.GetItem<bool?>(JavascriptDisabledDialogConstants.SuppressLocalStorageKey))
            .ReturnsAsync((bool?)null);

        Assert.That(await _sut.ShouldAutoOpen(), Is.True);
    }

    [Test]
    public async Task ShallNotOpenWhenJavascriptEnabled()
    {
        _accountService.SetupGet(x => x.AuthenticatedUser).Returns(new AuthenticatedUserModel
        {
            Id = Guid.NewGuid(),
            Role = Role.Administrator,
            Username = "admin"
        });
        _configurationFacade.SetupGet(x => x.WebFeaturesLoaded).Returns(true);
        _configurationFacade.SetupGet(x => x.AllowDisplayScripts).Returns(true);

        Assert.That(await _sut.ShouldAutoOpen(), Is.False);
    }

    [Test]
    public async Task ShallNotOpenWhenSuppressedInLocalStorage()
    {
        _accountService.SetupGet(x => x.AuthenticatedUser).Returns(new AuthenticatedUserModel
        {
            Id = Guid.NewGuid(),
            Role = Role.Administrator,
            Username = "admin"
        });
        _configurationFacade.SetupGet(x => x.WebFeaturesLoaded).Returns(true);
        _configurationFacade.SetupGet(x => x.AllowDisplayScripts).Returns(false);
        _localStorageService.Setup(x => x.GetItem<bool?>(JavascriptDisabledDialogConstants.SuppressLocalStorageKey))
            .ReturnsAsync(true);

        Assert.That(await _sut.ShouldAutoOpen(), Is.False);
    }

    [Test]
    public async Task SuppressPermanently_WritesLocalStorageKey()
    {
        await _sut.SuppressPermanently();

        _localStorageService.Verify(
            x => x.SetItem(JavascriptDisabledDialogConstants.SuppressLocalStorageKey, true),
            Times.Once);
    }
}
