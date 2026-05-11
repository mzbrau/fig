using Fig.Common.Events;
using Fig.Common.Timer;
using Fig.Contracts.Status;
using Fig.Web.Facades;
using Fig.Web.Services;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Web;

[TestFixture]
public class ApiVersionFacadeTests
{
    [Test]
    public async Task Constructor_ShouldPingApiWithoutJwtHeader()
    {
        var httpService = new Mock<IHttpService>();
        var timerFactory = new Mock<ITimerFactory>();
        var firstPingCompleted = new TaskCompletionSource<bool>();

        httpService.SetupGet(x => x.BaseAddress).Returns("https://localhost:5260");
        httpService.Setup(x => x.GetAnonymous<ApiVersionDataContract>("apiversion", false))
            .Callback(() => firstPingCompleted.TrySetResult(true))
            .ReturnsAsync(new ApiVersionDataContract("1.0.0", string.Empty, DateTime.UtcNow));

        timerFactory.Setup(x => x.Create(It.IsAny<TimeSpan>())).Returns(new StubPeriodicTimer());

        using var facade = new ApiVersionFacade(
            httpService.Object,
            timerFactory.Object,
            new EventDistributor(new Mock<ILogger<EventDistributor>>().Object));

        var completedTask = await Task.WhenAny(firstPingCompleted.Task, Task.Delay(TimeSpan.FromSeconds(1)));

        Assert.That(completedTask, Is.EqualTo(firstPingCompleted.Task), "Timed out waiting for the initial API version ping.");
        httpService.Verify(x => x.GetAnonymous<ApiVersionDataContract>("apiversion", false), Times.Once);
        httpService.Verify(x => x.Get<ApiVersionDataContract>("apiversion", false), Times.Never);
    }

    private sealed class StubPeriodicTimer : IPeriodicTimer
    {
        public ValueTask<bool> WaitForNextTickAsync(CancellationToken token) => ValueTask.FromResult(false);

        public void Dispose()
        {
        }
    }
}
