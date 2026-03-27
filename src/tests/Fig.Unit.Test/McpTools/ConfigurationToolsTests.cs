using Fig.Contracts.Configuration;
using Fig.Mcp.ApiClient;
using Fig.Mcp.Tools;
using Moq;
using NUnit.Framework;
using Newtonsoft.Json;

namespace Fig.Unit.Test.McpTools;

[TestFixture]
public class ConfigurationToolsTests
{
    [Test]
    public async Task GetConfiguration_CallsApiAndReturnsSerializedConfig()
    {
        var mock = new Mock<IFigApiClient>();
        var config = new FigConfigurationDataContract
        {
            AllowNewRegistrations = true,
            AllowUpdatedRegistrations = true,
            AllowFileImports = false,
            AllowOfflineSettings = true,
            AllowClientOverrides = false,
            ClientOverridesRegex = ".*",
            WebApplicationBaseAddress = "https://fig.example.com",
            UseAzureKeyVault = false,
            AzureKeyVaultName = null,
            PollIntervalOverride = 30.0,
            AllowDisplayScripts = true,
            EnableTimeMachine = false,
            TimelineDurationDays = 30
        };

        mock.Setup(x => x.GetConfigurationAsync())
            .ReturnsAsync(config);

        var result = await ConfigurationTools.GetConfiguration(mock.Object, CancellationToken.None);

        mock.Verify(x => x.GetConfigurationAsync(), Times.Once);
        Assert.That(result, Does.Contain("fig.example.com"));
        Assert.That(result, Does.Contain("AllowNewRegistrations"));
    }

    [Test]
    public async Task UpdateConfiguration_DeserializesJsonAndCallsApi()
    {
        var mock = new Mock<IFigApiClient>();
        var config = new FigConfigurationDataContract
        {
            AllowNewRegistrations = false,
            AllowUpdatedRegistrations = true,
            AllowFileImports = true,
            AllowOfflineSettings = false,
            AllowClientOverrides = true,
            ClientOverridesRegex = "^prod-.*",
            WebApplicationBaseAddress = "https://fig.production.com",
            UseAzureKeyVault = true,
            AzureKeyVaultName = "my-keyvault",
            PollIntervalOverride = 60.0,
            AllowDisplayScripts = false,
            EnableTimeMachine = true,
            TimelineDurationDays = 90
        };
        var configJson = JsonConvert.SerializeObject(config);

        mock.Setup(x => x.UpdateConfigurationAsync(
                It.IsAny<FigConfigurationDataContract>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var result = await ConfigurationTools.UpdateConfiguration(
            mock.Object, configJson, CancellationToken.None);

        mock.Verify(x => x.UpdateConfigurationAsync(
            It.Is<FigConfigurationDataContract>(c =>
                c.AllowNewRegistrations == false &&
                c.UseAzureKeyVault == true &&
                c.AzureKeyVaultName == "my-keyvault" &&
                c.TimelineDurationDays == 90),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.That(result, Does.Contain("onfiguration").IgnoreCase);
    }
}
