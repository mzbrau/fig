using Fig.Contracts.Scheduling;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class SchedulingWriteToolsTests
{
    [Test]
    public async Task RescheduleDeferredChange_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var changeIdStr = "99001000-0000-0000-0000-000000000001";
        var changeId = Guid.Parse(changeIdStr);
        var newExecuteAtUtc = new DateTime(2024, 12, 25, 8, 0, 0, DateTimeKind.Utc);

        mock.Setup(x => x.RescheduleChangeAsync(
                changeId,
                It.Is<RescheduleDeferredChangeDataContract>(r => r.NewExecuteAtUtc == newExecuteAtUtc),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await SchedulingWriteTools.RescheduleDeferredChange(
            mock.Object, changeIdStr, newExecuteAtUtc, CancellationToken.None);

        mock.Verify(x => x.RescheduleChangeAsync(
            changeId,
            It.Is<RescheduleDeferredChangeDataContract>(r => r.NewExecuteAtUtc == newExecuteAtUtc),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain(changeIdStr));
    }

    [Test]
    public async Task DeleteDeferredChange_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var changeIdStr = "55042000-0000-0000-0000-000000000002";
        var changeId = Guid.Parse(changeIdStr);

        mock.Setup(x => x.DeleteScheduledChangeAsync(changeId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await SchedulingWriteTools.DeleteDeferredChange(
            mock.Object, changeIdStr, CancellationToken.None);

        mock.Verify(x => x.DeleteScheduledChangeAsync(changeId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain(changeIdStr));
    }
}
