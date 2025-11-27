using System.Collections.Generic;
using Fig.Client.ExternallyManaged;
using Fig.Contracts.Status;
using NUnit.Framework;

namespace Fig.Unit.Test.Client.ExternallyManaged;

[TestFixture]
public class ExternallyManagedSettingsBridgeTests
{
    [TearDown]
    public void TearDown()
    {
        ExternallyManagedSettingsBridge.SetExternallyManagedSettings(null);
    }

    [Test]
    public void SetExternallyManagedSettings_ShouldStoreSettings()
    {
        // Arrange
        var settings = new List<ExternallyManagedSettingDataContract>
        {
            new("Setting1", "Value1"),
            new("Setting2", "Value2")
        };

        // Act
        ExternallyManagedSettingsBridge.SetExternallyManagedSettings(settings);

        // Assert
        Assert.That(ExternallyManagedSettingsBridge.ExternallyManagedSettings, Is.Not.Null);
        Assert.That(ExternallyManagedSettingsBridge.ExternallyManagedSettings!.Count, Is.EqualTo(2));
    }

    [Test]
    public void ConsumeExternallyManagedSettings_ShouldReturnAndClearSettings()
    {
        // Arrange
        var settings = new List<ExternallyManagedSettingDataContract>
        {
            new("Setting1", "Value1")
        };
        ExternallyManagedSettingsBridge.SetExternallyManagedSettings(settings);

        // Act
        var result = ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings();

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Count, Is.EqualTo(1));
        Assert.That(ExternallyManagedSettingsBridge.ExternallyManagedSettings, Is.Null);
    }

    [Test]
    public void ConsumeExternallyManagedSettings_CalledTwice_ShouldReturnNullOnSecondCall()
    {
        // Arrange
        var settings = new List<ExternallyManagedSettingDataContract>
        {
            new("Setting1", "Value1")
        };
        ExternallyManagedSettingsBridge.SetExternallyManagedSettings(settings);

        // Act
        var firstResult = ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings();
        var secondResult = ExternallyManagedSettingsBridge.ConsumeExternallyManagedSettings();

        // Assert
        Assert.That(firstResult, Is.Not.Null);
        Assert.That(secondResult, Is.Null);
    }
}
