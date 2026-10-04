using Fig.Common.Events;
using Fig.Contracts.Status;
using Fig.Web.Converters;
using Fig.Web.Facades;
using Fig.Web.Models.Clients;
using Fig.Web.Services;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class ClientStatusFacadeTests
{
    private Mock<IHttpService> _httpService = null!;
    private Mock<IClientRunSessionConverter> _converter = null!;
    private Mock<IEventDistributor> _eventDistributor = null!;
    private ClientStatusFacade _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _httpService = new Mock<IHttpService>();
        _converter = new Mock<IClientRunSessionConverter>();
        _eventDistributor = new Mock<IEventDistributor>();
        _sut = new ClientStatusFacade(_httpService.Object, _converter.Object, _eventDistributor.Object);
    }

    [Test]
    public async Task Refresh_ReturnsFalse_WhenHttpServiceReturnsNull()
    {
        _httpService.Setup(x => x.Get<List<ClientStatusDataContract>>("statuses", true))
            .ReturnsAsync((List<ClientStatusDataContract>?)null);

        var result = await _sut.Refresh();

        Assert.That(result, Is.False);
        Assert.That(_sut.ClientRunSessions, Is.Empty);
        _converter.Verify(x => x.Convert(It.IsAny<List<ClientStatusDataContract>>()), Times.Never);
    }

    [Test]
    public async Task Refresh_ReturnsTrueAndUpdatesSessions_WhenHttpServiceReturnsData()
    {
        var statuses = new List<ClientStatusDataContract>
        {
            new("ClientA", null, DateTime.UtcNow, null, Array.Empty<ClientRunSessionDataContract>())
        };
        var sessions = new List<ClientRunSessionModel>();
        _httpService.Setup(x => x.Get<List<ClientStatusDataContract>>("statuses", true))
            .ReturnsAsync(statuses);
        _converter.Setup(x => x.Convert(statuses)).Returns(sessions);

        var result = await _sut.Refresh();

        Assert.That(result, Is.True);
        Assert.That(_sut.ClientRunSessions, Is.Empty);
        _converter.Verify(x => x.Convert(statuses), Times.Once);
        _eventDistributor.Verify(x => x.PublishAsync(It.IsAny<string>()), Times.Once);
    }
}
