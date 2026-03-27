using Fig.Contracts.CustomActions;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class CustomActionWriteToolsTests
{
    [Test]
    public async Task ExecuteCustomAction_WithRunSessionId_CallsApiWithSessionId()
    {
        var mock = new Mock<IFigApiClient>();
        var sessionId = Guid.NewGuid();
        var response = new CustomActionExecutionResponseDataContract(
            Guid.NewGuid(), "Action executed successfully", false);

        mock.Setup(x => x.ExecuteCustomActionAsync(
                It.IsAny<string>(),
                It.IsAny<CustomActionExecutionRequestDataContract>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CustomActionWriteTools.ExecuteCustomAction(
            mock.Object, "MyClient", "RestartService", sessionId.ToString(), CancellationToken.None);

        mock.Verify(x => x.ExecuteCustomActionAsync(
            "MyClient",
            It.Is<CustomActionExecutionRequestDataContract>(r =>
                r.CustomActionName == "RestartService" &&
                r.RunSessionId == sessionId),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result, Does.Contain("Action executed successfully"));
    }

    [Test]
    public async Task ExecuteCustomAction_WithoutRunSessionId_CallsApiWithNullSessionId()
    {
        var mock = new Mock<IFigApiClient>();
        var response = new CustomActionExecutionResponseDataContract(
            Guid.NewGuid(), "Action queued", true);

        mock.Setup(x => x.ExecuteCustomActionAsync(
                It.IsAny<string>(),
                It.IsAny<CustomActionExecutionRequestDataContract>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        var result = await CustomActionWriteTools.ExecuteCustomAction(
            mock.Object, "TestClient", "ClearCache", null, CancellationToken.None);

        mock.Verify(x => x.ExecuteCustomActionAsync(
            "TestClient",
            It.Is<CustomActionExecutionRequestDataContract>(r =>
                r.CustomActionName == "ClearCache" &&
                r.RunSessionId == null),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result, Does.Contain("Action queued"));
    }
}
