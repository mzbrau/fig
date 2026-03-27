using Fig.Contracts.CheckPoint;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class TimeMachineReadToolsTests
{
    [Test]
    public async Task ListCheckPoints_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var startTime = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var endTime = new DateTime(2024, 6, 30, 23, 59, 59, DateTimeKind.Utc);

        var dataId = Guid.NewGuid();
        var checkPoints = new List<CheckPointDataContract>
        {
            new(Guid.NewGuid(), dataId, new DateTime(2024, 3, 15, 10, 0, 0, DateTimeKind.Utc),
                5, 42, "SettingChanged", "Initial deployment", "admin"),
            new(Guid.NewGuid(), Guid.NewGuid(), new DateTime(2024, 5, 20, 14, 30, 0, DateTimeKind.Utc),
                3, 18, "ClientRegistered", null, "deployer")
        };

        var checkPointCollection = new CheckPointCollectionDataContract(startTime, startTime, endTime, checkPoints);

        mock.Setup(x => x.GetCheckPointsAsync(startTime, endTime, It.IsAny<CancellationToken>()))
            .ReturnsAsync(checkPointCollection);

        var result = await TimeMachineReadTools.ListCheckPoints(
            mock.Object, startTime, endTime, CancellationToken.None);

        mock.Verify(x => x.GetCheckPointsAsync(startTime, endTime, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("SettingChanged"));
        Assert.That(result, Does.Contain("Initial deployment"));
        Assert.That(result, Does.Contain("ClientRegistered"));
        Assert.That(result, Does.Contain(dataId.ToString()));
    }

    [Test]
    public async Task GetCheckPointData_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var dataId = Guid.NewGuid();
        var expectedData = "{\"settings\":[{\"name\":\"MaxRetries\",\"value\":3}]}";

        mock.Setup(x => x.GetCheckPointDataAsync(dataId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedData);

        var result = await TimeMachineReadTools.GetCheckPointData(
            mock.Object, dataId.ToString(), CancellationToken.None);

        mock.Verify(x => x.GetCheckPointDataAsync(dataId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Is.EqualTo(expectedData));
    }
}
