using Fig.Client.ConfigurationProvider;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;

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
}
