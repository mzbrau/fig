using Fig.Contracts.CheckPoint;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class TimeMachineWriteToolsTests
{
    [Test]
    public async Task ApplyCheckPoint_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var checkPointIdStr = "12345000-0000-0000-0000-000000000001";
        var checkPointId = Guid.Parse(checkPointIdStr);

        mock.Setup(x => x.ApplyCheckPointAsync(checkPointId, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await TimeMachineWriteTools.ApplyCheckPoint(
            mock.Object, checkPointIdStr, CancellationToken.None);

        mock.Verify(x => x.ApplyCheckPointAsync(checkPointId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain(checkPointIdStr));
    }

    [Test]
    public async Task UpdateCheckPointNote_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var checkPointIdStr = "67890000-0000-0000-0000-000000000002";
        var checkPointId = Guid.Parse(checkPointIdStr);
        var note = "Production rollback checkpoint";

        mock.Setup(x => x.UpdateCheckPointNoteAsync(
                checkPointId,
                It.Is<CheckPointUpdateDataContract>(u => u.Note == note),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await TimeMachineWriteTools.UpdateCheckPointNote(
            mock.Object, checkPointIdStr, note, CancellationToken.None);

        mock.Verify(x => x.UpdateCheckPointNoteAsync(
            checkPointId,
            It.Is<CheckPointUpdateDataContract>(u => u.Note == note),
            It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain(checkPointIdStr));
    }
}
