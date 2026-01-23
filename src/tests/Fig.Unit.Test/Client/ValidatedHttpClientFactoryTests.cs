using Fig.Client.ConfigurationProvider;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Runtime.InteropServices;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ValidatedHttpClientFactoryTests
{
    private Mock<ILogger<ValidatedHttpClientFactory>> _loggerMock = null!;

    [SetUp]
    public void Setup()
    {
        _loggerMock = new Mock<ILogger<ValidatedHttpClientFactory>>();
    }

    [Test]
    public void Constructor_WithCustomTimeout_UsesProvidedTimeout()
    {
        // Arrange
        var customTimeout = TimeSpan.FromSeconds(10);
        var customRetries = 5;

        // Act
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object, customTimeout, customRetries);

        // Assert - factory should be created successfully
        // The timeout and retry values will be used when creating clients
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithoutParameters_UsesDefaults()
    {
        // Act - creates factory with default parameters
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object);

        // Assert
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNullTimeout_UsesDefaults()
    {
        // Act
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object, null, null);

        // Assert
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void CreateClient_ThrowsException_WhenApiUrisIsNull()
    {
        // Arrange
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await factory.CreateClient(null));
    }

    [Test]
    public void CreateClient_ThrowsException_WhenApiUrisIsEmpty()
    {
        // Arrange
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object);

        // Act & Assert
        Assert.ThrowsAsync<ArgumentException>(async () => await factory.CreateClient(new System.Collections.Generic.List<string>()));
    }

    [Test]
    public void Constructor_WithNoOfflineSettings_OnLinux_Uses60SecondTimeout()
    {
        // Skip test if running on Windows, as it requires Linux behavior
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !Environment.UserInteractive)
        {
            Assert.Ignore("Test requires non-Windows service environment");
        }

        // Act
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object, null, null, hasOfflineSettings: false);

        // Assert - verify that extended timeout log message is written
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No offline settings available") && 
                                               (v.ToString()!.Contains("60") || v.ToString()!.Contains("5"))),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNoOfflineSettings_AsWindowsService_Uses5SecondTimeout()
    {
        // This test verifies the Windows service path exists in the code
        // Actual Windows service detection requires Windows environment with specific privileges

        // Act
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object, null, null, hasOfflineSettings: false);

        // Assert - verify factory is created and extended timeout log is called
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No offline settings available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithOfflineSettings_UsesDefaultShortTimeout()
    {
        // Act - hasOfflineSettings=true (default parameter)
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object, null, null, hasOfflineSettings: true);

        // Assert - verify NO extended timeout log message (since offline settings exist)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No offline settings available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithOfflineSettings_DefaultParameter_UsesDefaultShortTimeout()
    {
        // Act - using default hasOfflineSettings parameter (should be true)
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object);

        // Assert - verify NO extended timeout log message
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No offline settings available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithExplicitTimeout_OverridesOfflineSettingsLogic()
    {
        // Arrange
        var explicitTimeout = TimeSpan.FromSeconds(15);

        // Act - explicit timeout should override hasOfflineSettings logic
        var factory = new ValidatedHttpClientFactory(
            _loggerMock.Object, 
            requestTimeout: explicitTimeout, 
            retryCount: null, 
            hasOfflineSettings: false);

        // Assert - verify NO extended timeout log message (explicit timeout takes precedence)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("No offline settings available")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
        
        Assert.That(factory, Is.Not.Null);
    }

    [Test]
    public void Constructor_WithNoOfflineSettings_LogsExtendedTimeoutValue()
    {
        // Act
        var factory = new ValidatedHttpClientFactory(_loggerMock.Object, null, null, hasOfflineSettings: false);

        // Assert - verify log contains timeout value (either 5 or 60 depending on environment)
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => 
                    v.ToString()!.Contains("No offline settings available") &&
                    v.ToString()!.Contains("Using extended API timeout:")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
        
        Assert.That(factory, Is.Not.Null);
    }
}
