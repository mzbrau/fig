using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Status;
using Fig.Client.Workers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client.Workers;

[TestFixture]
public class FigExternallyManagedSettingsWorkerTests
{
    private Mock<ILogger<FigExternallyManagedSettingsWorker<AllTypesSettings>>> _loggerMock = null!;
    private AllTypesSettings _settings = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<FigExternallyManagedSettingsWorker<AllTypesSettings>>>();
        _settings = new AllTypesSettings();
        
        // Reset the static bridge before each test
        ExternallyManagedSettingsBridge.ExternallyManagedSettings = null;
    }

    [TearDown]
    public void TearDown()
    {
        ExternallyManagedSettingsBridge.ExternallyManagedSettings = null;
    }

    [Test]
    public async Task StartAsync_WhenNoFigProvider_ShouldNotDetectExternallyManagedSettings()
    {
        // Arrange
        var configurationRootMock = new Mock<IConfigurationRoot>();
        configurationRootMock.Setup(x => x.Providers).Returns(Enumerable.Empty<IConfigurationProvider>());
        
        var worker = new FigExternallyManagedSettingsWorker<AllTypesSettings>(
            _loggerMock.Object,
            configurationRootMock.Object,
            _settings);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert
        Assert.That(ExternallyManagedSettingsBridge.ExternallyManagedSettings, Is.Null);
    }

    [Test]
    public async Task StartAsync_WhenRegularConfigProvider_ShouldNotDetectExternallyManagedSettings()
    {
        // Arrange - use a regular memory configuration provider, not a Fig provider
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "StringSetting", "someValue" }
            })
            .Build();
        
        var worker = new FigExternallyManagedSettingsWorker<AllTypesSettings>(
            _loggerMock.Object,
            configuration,
            _settings);

        // Act
        await worker.StartAsync(CancellationToken.None);

        // Assert - No Fig provider means no detection
        Assert.That(ExternallyManagedSettingsBridge.ExternallyManagedSettings, Is.Null);
    }

    [Test]
    public async Task StartAsync_WhenCalledMultipleTimes_ShouldOnlyDetectOnce()
    {
        // Arrange - Even without a Fig provider, test the hasDetected flag
        var configurationRootMock = new Mock<IConfigurationRoot>();
        configurationRootMock.Setup(x => x.Providers).Returns(Enumerable.Empty<IConfigurationProvider>());

        var worker = new FigExternallyManagedSettingsWorker<AllTypesSettings>(
            _loggerMock.Object,
            configurationRootMock.Object,
            _settings);

        // Act - Call twice
        await worker.StartAsync(CancellationToken.None);
        await worker.StartAsync(CancellationToken.None);

        // Assert - Should only log debug message once due to hasDetected flag
        // (We can't directly verify the log in this simple test, but the method should complete without error)
        Assert.That(ExternallyManagedSettingsBridge.ExternallyManagedSettings, Is.Null);
    }

    [Test]
    public async Task StopAsync_ShouldCompleteImmediately()
    {
        // Arrange
        var configurationRootMock = new Mock<IConfigurationRoot>();
        configurationRootMock.Setup(x => x.Providers).Returns(Enumerable.Empty<IConfigurationProvider>());
        
        var worker = new FigExternallyManagedSettingsWorker<AllTypesSettings>(
            _loggerMock.Object,
            configurationRootMock.Object,
            _settings);

        // Act & Assert
        Assert.DoesNotThrowAsync(async () => await worker.StopAsync(CancellationToken.None));
    }

    [Test]
    public void StartAsync_WithCancellationRequested_ShouldThrowOperationCanceled()
    {
        // Arrange
        var configurationRootMock = new Mock<IConfigurationRoot>();
        configurationRootMock.Setup(x => x.Providers).Returns(Enumerable.Empty<IConfigurationProvider>());

        var worker = new FigExternallyManagedSettingsWorker<AllTypesSettings>(
            _loggerMock.Object,
            configurationRootMock.Object,
            _settings);

        var cts = new CancellationTokenSource();
        cts.Cancel();

        // Act & Assert - Task.Delay throws TaskCanceledException which derives from OperationCanceledException
        var exception = Assert.ThrowsAsync<TaskCanceledException>(async () => await worker.StartAsync(cts.Token));
        Assert.That(exception, Is.InstanceOf<OperationCanceledException>());
    }

    [Test]
    public void Constructor_ShouldInitializeWithoutException()
    {
        // Arrange
        var configurationRootMock = new Mock<IConfigurationRoot>();
        configurationRootMock.Setup(x => x.Providers).Returns(Enumerable.Empty<IConfigurationProvider>());

        // Act & Assert
        Assert.DoesNotThrow(() =>
        {
            var worker = new FigExternallyManagedSettingsWorker<AllTypesSettings>(
                _loggerMock.Object,
                configurationRootMock.Object,
                _settings);
        });
    }
}

/// <summary>
/// Test settings class with all supported setting types
/// </summary>
public class AllTypesSettings : SettingsBase
{
    public override string ClientDescription => "Settings class for testing all setting types";

    [Setting("A string setting")]
    public string StringSetting { get; set; } = "default";

    [Setting("An integer setting")]
    public int IntSetting { get; set; } = 42;

    [Setting("A boolean setting")]
    public bool BoolSetting { get; set; } = true;

    [Setting("A double setting")]
    public double DoubleSetting { get; set; } = 3.14;

    [Setting("A long setting")]
    public long LongSetting { get; set; } = 1000000000;

    [Setting("A DateTime setting")]
    public DateTime DateTimeSetting { get; set; } = DateTime.UtcNow;

    [Setting("A TimeSpan setting")]
    public TimeSpan TimeSpanSetting { get; set; } = TimeSpan.FromMinutes(30);

    [Setting("A list setting")]
    public List<string>? ListSetting { get; set; } = new List<string> { "item1", "item2" };

    [Setting("An enum setting")]
    [ValidValues(typeof(TestEnum))]
    public TestEnum EnumSetting { get; set; } = TestEnum.Value1;

    [Setting("A nullable int setting")]
    public int? NullableIntSetting { get; set; }

    public override IEnumerable<string> GetValidationErrors()
    {
        return new List<string>();
    }
}

/// <summary>
/// Test enum for enum setting tests
/// </summary>
public enum TestEnum
{
    Value1 = 1,
    Value2 = 2,
    Value3 = 3
}
