using System;
using Fig.Api;
using NUnit.Framework;
using System.Threading.RateLimiting;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class RateLimitingSettingsTests
{
    [Test]
    public void RateLimitingSettings_DefaultConstructor_SetsExpectedDefaults()
    {
        // Arrange & Act
        var settings = new RateLimitingSettings();

        // Assert
        Assert.That(settings.GlobalPolicy, Is.Not.Null);
        Assert.That(settings.GlobalPolicy.Enabled, Is.True);
        Assert.That(settings.GlobalPolicy.PermitLimit, Is.EqualTo(500));
        Assert.That(settings.GlobalPolicy.Window, Is.EqualTo(TimeSpan.FromMinutes(1)));
        Assert.That(settings.GlobalPolicy.ProcessingOrder, Is.EqualTo(QueueProcessingOrder.OldestFirst));
        Assert.That(settings.GlobalPolicy.QueueLimit, Is.EqualTo(10));
    }

    [Test]
    public void GlobalPolicySettings_DefaultConstructor_SetsExpectedDefaults()
    {
        // Arrange & Act
        var settings = new GlobalPolicySettings();

        // Assert
        Assert.That(settings.Enabled, Is.True);
        Assert.That(settings.PermitLimit, Is.EqualTo(500));
        Assert.That(settings.Window, Is.EqualTo(TimeSpan.FromMinutes(1)));
        Assert.That(settings.ProcessingOrder, Is.EqualTo(QueueProcessingOrder.OldestFirst));
        Assert.That(settings.QueueLimit, Is.EqualTo(10));
    }

    [Test]
    public void GlobalPolicySettings_CustomValues_AreSetCorrectly()
    {
        // Arrange & Act
        var settings = new GlobalPolicySettings
        {
            Enabled = false,
            PermitLimit = 50,
            Window = TimeSpan.FromMinutes(5),
            ProcessingOrder = QueueProcessingOrder.NewestFirst,
            QueueLimit = 5
        };

        // Assert
        Assert.That(settings.Enabled, Is.False);
        Assert.That(settings.PermitLimit, Is.EqualTo(50));
        Assert.That(settings.Window, Is.EqualTo(TimeSpan.FromMinutes(5)));
        Assert.That(settings.ProcessingOrder, Is.EqualTo(QueueProcessingOrder.NewestFirst));
        Assert.That(settings.QueueLimit, Is.EqualTo(5));
    }
}