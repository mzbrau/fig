using Fig.Contracts.CustomActions;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class CustomActionReadToolsTests
{
    [Test]
    public async Task GetCustomActionStatus_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var executionId = Guid.NewGuid();
        var requestedAt = new DateTime(2024, 7, 10, 9, 0, 0, DateTimeKind.Utc);
        var executedAt = new DateTime(2024, 7, 10, 9, 0, 5, DateTimeKind.Utc);
        var runSessionId = Guid.NewGuid();

        var status = new CustomActionExecutionStatusDataContract(
            executionId,
            ExecutionStatus.Completed,
            requestedAt,
            executedAt,
            new List<CustomActionResultDataContract>(),
            true,
            runSessionId);

        mock.Setup(x => x.GetCustomActionStatusAsync(executionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(status);

        var result = await CustomActionReadTools.GetCustomActionStatus(
            mock.Object, executionId.ToString(), CancellationToken.None);

        mock.Verify(x => x.GetCustomActionStatusAsync(executionId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain(executionId.ToString()));
        Assert.That(result, Does.Contain("\"Succeeded\": true"));
    }

    [Test]
    public async Task GetCustomActionHistory_CallsCorrectApiMethod()
    {
        var mock = new Mock<IFigApiClient>();
        var clientName = "OrderProcessingService";
        var customActionId = Guid.NewGuid();
        var executionId = Guid.NewGuid();
        var requestedAt = new DateTime(2024, 8, 5, 14, 0, 0, DateTimeKind.Utc);

        var executions = new List<CustomActionExecutionStatusDataContract>
        {
            new(executionId,
                ExecutionStatus.SentToClient,
                requestedAt,
                null,
                null,
                false,
                null)
        };

        var history = new CustomActionExecutionHistoryDataContract(
            clientName, "ClearCache", executions);

        mock.Setup(x => x.GetCustomActionHistoryAsync(clientName, customActionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(history);

        var result = await CustomActionReadTools.GetCustomActionHistory(
            mock.Object, clientName, customActionId.ToString(), CancellationToken.None);

        mock.Verify(x => x.GetCustomActionHistoryAsync(
            clientName, customActionId, It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Does.Contain("OrderProcessingService"));
        Assert.That(result, Does.Contain("ClearCache"));
        Assert.That(result, Does.Contain(executionId.ToString()));
    }
}
