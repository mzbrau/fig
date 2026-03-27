using Fig.Contracts.Scheduling;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class SchedulingReadToolsTests
{
    [Test]
    public async Task ListDeferredChanges_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var schedulingData = new SchedulingChangesDataContract
        {
            Changes = new List<DeferredChangeDataContract>()
        };

        mock.Setup(x => x.GetDeferredChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(schedulingData);

        var result = await SchedulingReadTools.ListDeferredChanges(
            mock.Object, CancellationToken.None);

        mock.Verify(x => x.GetDeferredChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        var deserialized = JsonConvert.DeserializeObject<SchedulingChangesDataContract>(result);
        Assert.That(deserialized, Is.Not.Null);
        Assert.That(deserialized!.Changes, Is.Not.Null);
    }
}
