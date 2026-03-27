using Fig.Contracts.ClientRegistrationHistory;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Status;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class StatusToolsTests
{
    private Mock<IFigApiClient> _apiClient = null!;

    [SetUp]
    public void SetUp()
    {
        _apiClient = new Mock<IFigApiClient>();
    }

    [Test]
    public async Task GetApiVersion_ShouldReturnVersionAndLastSettingChange()
    {
        var versionContract = new ApiVersionDataContract("3.2.1", string.Empty, new DateTime(2025, 6, 15, 10, 30, 0, DateTimeKind.Utc));
        _apiClient.Setup(x => x.GetApiVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(versionContract);

        var result = await StatusTools.GetApiVersion(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetApiVersionAsync(It.IsAny<CancellationToken>()), Times.Once);
        var json = JObject.Parse(result);
        Assert.That(json["ApiVersion"]!.Value<string>(), Is.EqualTo("3.2.1"));
        Assert.That(json["LastSettingChange"], Is.Not.Null);
    }

    [Test]
    public async Task GetApiStatus_ShouldReturnStatusCollection()
    {
        var statuses = new List<ApiStatusDataContract>
        {
            new(Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "127.0.0.1", "host1", 1024, "user", 100, 5.0, "3.2.1", false)
        };
        _apiClient.Setup(x => x.GetApiStatusAsync(It.IsAny<CancellationToken>())).ReturnsAsync(statuses);

        var result = await StatusTools.GetApiStatus(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetApiStatusAsync(It.IsAny<CancellationToken>()), Times.Once);
        var json = JArray.Parse(result);
        Assert.That(json, Has.Count.EqualTo(1));
        Assert.That(json[0]["Version"]!.Value<string>(), Is.EqualTo("3.2.1"));
    }

    [Test]
    public async Task GetDeferredImports_ShouldCallApi_AndReturnSerializedJson()
    {
        var imports = new List<DeferredImportClientDataContract>
        {
            new("ClientA", null, 5, "admin"),
            new("ClientB", "staging", 3, "user1")
        };
        _apiClient.Setup(x => x.GetDeferredImportsAsync(It.IsAny<CancellationToken>())).ReturnsAsync(imports);

        var result = await StatusTools.GetDeferredImports(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetDeferredImportsAsync(It.IsAny<CancellationToken>()), Times.Once);
        var deserialized = JsonConvert.DeserializeObject<List<DeferredImportClientDataContract>>(result);
        Assert.That(deserialized, Has.Count.EqualTo(2));
        Assert.That(deserialized![0].Name, Is.EqualTo("ClientA"));
        Assert.That(deserialized![1].SettingCount, Is.EqualTo(3));
    }

    [Test]
    public async Task GetClientRegistrationHistory_ShouldCallApi_AndReturnSerializedJson()
    {
        var history = new ClientRegistrationHistoryCollectionDataContract();
        _apiClient.Setup(x => x.GetClientRegistrationHistoryAsync(It.IsAny<CancellationToken>())).ReturnsAsync(history);

        var result = await StatusTools.GetClientRegistrationHistory(_apiClient.Object, CancellationToken.None);

        _apiClient.Verify(x => x.GetClientRegistrationHistoryAsync(It.IsAny<CancellationToken>()), Times.Once);
        Assert.That(result, Is.Not.Null.And.Not.Empty);
        Assert.DoesNotThrow(() => JsonConvert.DeserializeObject(result));
    }

    [Test]
    public async Task GetApiVersion_ShouldProduceValidJson()
    {
        var versionContract = new ApiVersionDataContract("1.0.0", string.Empty, DateTime.UtcNow);
        _apiClient.Setup(x => x.GetApiVersionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(versionContract);

        var result = await StatusTools.GetApiVersion(_apiClient.Object, CancellationToken.None);

        Assert.That(result, Does.Contain("\"ApiVersion\""));
        Assert.That(result, Does.Contain("\"1.0.0\""));
    }
}
