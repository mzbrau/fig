using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client;
using Fig.Client.ExternallyManaged;
using Fig.Contracts.Status;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client.ExternallyManaged;

[TestFixture]
public class FigExternallyManagedWorkerTests
{
    private Mock<IConfiguration> _configurationMock = null!;

    [SetUp]
    public void Setup()
    {
        _configurationMock = new Mock<IConfiguration>();
        
        // Clear any previous state
        FigValuesStore.Clear();
        ExternallyManagedSettingsBridge.SetExternallyManagedSettings(null);
    }

    [TearDown]
    public void TearDown()
    {
        FigValuesStore.Clear();
        ExternallyManagedSettingsBridge.SetExternallyManagedSettings(null);
    }

    [Test]
    public async Task StartAsync_WhenNoFigValues_ShouldNotSetExternallyManagedSettings()
    {
        // Arrange
        var worker = new FigExternallyManagedWorker<TestSettingsPublic>(
            _configurationMock.Object,
            NullLogger<FigExternallyManagedWorker<TestSettingsPublic>>.Instance);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        Assert.That(ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings(), Is.Null);
    }

    [Test]
    public async Task StartAsync_WhenValuesMatch_ShouldNotSetExternallyManagedSettings()
    {
        // Arrange
        var figValues = new Dictionary<string, string?>
        {
            ["Setting1"] = "Value1",
            ["Setting2"] = "Value2"
        };
        FigValuesStore.StoreFigValues(figValues);

        _configurationMock.Setup(x => x["Setting1"]).Returns("Value1");
        _configurationMock.Setup(x => x["Setting2"]).Returns("Value2");

        var worker = new FigExternallyManagedWorker<TestSettingsPublic>(
            _configurationMock.Object,
            NullLogger<FigExternallyManagedWorker<TestSettingsPublic>>.Instance);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        var result = ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings();
        Assert.That(result, Is.Null.Or.Empty);
    }

    [Test]
    public async Task StartAsync_WhenValuesDiffer_ShouldSetExternallyManagedSettings()
    {
        // Arrange
        var figValues = new Dictionary<string, string?>
        {
            ["Setting1"] = "FigValue1",
            ["Setting2"] = "FigValue2"
        };
        FigValuesStore.StoreFigValues(figValues);

        _configurationMock.Setup(x => x["Setting1"]).Returns("OverriddenValue1");
        _configurationMock.Setup(x => x["Setting2"]).Returns("FigValue2"); // This matches

        var worker = new FigExternallyManagedWorker<TestSettingsPublic>(
            _configurationMock.Object,
            NullLogger<FigExternallyManagedWorker<TestSettingsPublic>>.Instance);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        var result = ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("Setting1"));
        Assert.That(result[0].Value, Is.EqualTo("OverriddenValue1"));
    }

    [Test]
    public async Task StartAsync_ShouldExcludeMetadataProperties()
    {
        // Arrange
        var figValues = new Dictionary<string, string?>
        {
            ["LastFigUpdateUtcTicks"] = "12345",
            ["FigSettingLoadType"] = "Server",
            ["RestartRequested"] = "false",
            ["RealSetting"] = "FigValue"
        };
        FigValuesStore.StoreFigValues(figValues);

        // All values differ from configuration
        _configurationMock.Setup(x => x["LastFigUpdateUtcTicks"]).Returns("different");
        _configurationMock.Setup(x => x["FigSettingLoadType"]).Returns("different");
        _configurationMock.Setup(x => x["RestartRequested"]).Returns("true");
        _configurationMock.Setup(x => x["RealSetting"]).Returns("OverriddenValue");

        var worker = new FigExternallyManagedWorker<TestSettingsPublic>(
            _configurationMock.Object,
            NullLogger<FigExternallyManagedWorker<TestSettingsPublic>>.Instance);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        var result = ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings();
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
        Assert.That(result[0].Name, Is.EqualTo("RealSetting"));
    }

    [Test]
    public async Task StartAsync_CalledMultipleTimes_ShouldOnlyCheckOnce()
    {
        // Arrange
        var figValues = new Dictionary<string, string?>
        {
            ["Setting1"] = "FigValue1"
        };
        FigValuesStore.StoreFigValues(figValues);

        _configurationMock.Setup(x => x["Setting1"]).Returns("OverriddenValue1");

        var worker = new FigExternallyManagedWorker<TestSettingsPublic>(
            _configurationMock.Object,
            NullLogger<FigExternallyManagedWorker<TestSettingsPublic>>.Instance);

        // Act - first call
        await worker.StartAsync(CancellationToken.None);
        var firstResult = ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings();

        // Consume and call again
        await worker.StartAsync(CancellationToken.None);
        var secondResult = ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings();

        // Assert
        Assert.That(firstResult, Is.Not.Null);
        Assert.That(firstResult!.Count, Is.EqualTo(1));
        Assert.That(secondResult, Is.Null); // Should not produce more results
    }
}

// Public test settings class for the generic worker
public class TestSettingsPublic : SettingsBase
{
    public override string ClientDescription => "Test Settings";
    public override IEnumerable<string> GetValidationErrors() => [];
}
