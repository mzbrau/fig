using System;
using System.Collections.Generic;
using Fig.Api.Enums;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Datalayer.BusinessEntities;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class RegistrationStatusValidatorTests
{
    private Mock<IHashValidationCache> _mockHashValidationCache = null!;
    private RegistrationStatusValidator _validator = null!;

    [SetUp]
    public void SetUp()
    {
        _mockHashValidationCache = new Mock<IHashValidationCache>();
        _validator = new RegistrationStatusValidator(_mockHashValidationCache.Object);
    }

    [Test]
    public void GetStatus_WithNullExistingRegistrations_ShouldReturnNoExistingRegistrations()
    {
        // Act
        var result = _validator.GetStatus((List<SettingClientBusinessEntity>?)null, "secret");

        // Assert
        Assert.That(result, Is.EqualTo(CurrentRegistrationStatus.NoExistingRegistrations));
    }

    [Test]
    public void GetStatus_WithEmptyExistingRegistrations_ShouldReturnNoExistingRegistrations()
    {
        // Act
        var result = _validator.GetStatus(new List<SettingClientBusinessEntity>(), "secret");

        // Assert
        Assert.That(result, Is.EqualTo(CurrentRegistrationStatus.NoExistingRegistrations));
    }

    [Test]
    public void GetStatus_WithMatchingSecret_ShouldReturnMatchesExistingSecret()
    {
        // Arrange
        var client = new SettingClientBusinessEntity
        {
            Name = "TestClient",
            ClientSecret = "hashed-secret"
        };
        _mockHashValidationCache
            .Setup(x => x.ValidateClientSecret("TestClient", "correct-secret", "hashed-secret"))
            .Returns(true);

        // Act
        var result = _validator.GetStatus(client, "correct-secret");

        // Assert
        Assert.That(result, Is.EqualTo(CurrentRegistrationStatus.MatchesExistingSecret));
    }

    [Test]
    public void GetStatus_WithNonMatchingSecret_ShouldReturnDoesNotMatchSecret()
    {
        // Arrange
        var client = new SettingClientBusinessEntity
        {
            Name = "TestClient",
            ClientSecret = "hashed-secret",
            PreviousClientSecret = null,
            PreviousClientSecretExpiryUtc = null
        };
        _mockHashValidationCache
            .Setup(x => x.ValidateClientSecret("TestClient", "wrong-secret", "hashed-secret"))
            .Returns(false);

        // Act
        var result = _validator.GetStatus(client, "wrong-secret");

        // Assert
        Assert.That(result, Is.EqualTo(CurrentRegistrationStatus.DoesNotMatchSecret));
    }

    [Test]
    public void GetStatus_InSecretChangePeriod_WithMatchingPreviousSecret_ShouldReturnIsWithinChangePeriodAndMatchesPreviousSecret()
    {
        // Arrange
        var client = new SettingClientBusinessEntity
        {
            Name = "TestClient",
            ClientSecret = "new-hashed-secret",
            PreviousClientSecret = "old-hashed-secret",
            PreviousClientSecretExpiryUtc = DateTime.UtcNow.AddHours(1) // Still in change period
        };
        
        // Current secret doesn't match
        _mockHashValidationCache
            .Setup(x => x.ValidateClientSecret("TestClient", "old-secret", "new-hashed-secret"))
            .Returns(false);
        
        // Previous secret matches
        _mockHashValidationCache
            .Setup(x => x.ValidateClientSecret("TestClient:previous", "old-secret", "old-hashed-secret"))
            .Returns(true);

        // Act
        var result = _validator.GetStatus(client, "old-secret");

        // Assert
        Assert.That(result, Is.EqualTo(CurrentRegistrationStatus.IsWithinChangePeriodAndMatchesPreviousSecret));
    }

    [Test]
    public void GetStatus_ExpiredSecretChangePeriod_WithMatchingPreviousSecret_ShouldReturnDoesNotMatchSecret()
    {
        // Arrange
        var client = new SettingClientBusinessEntity
        {
            Name = "TestClient",
            ClientSecret = "new-hashed-secret",
            PreviousClientSecret = "old-hashed-secret",
            PreviousClientSecretExpiryUtc = DateTime.UtcNow.AddHours(-1) // Change period expired
        };
        
        // Current secret doesn't match
        _mockHashValidationCache
            .Setup(x => x.ValidateClientSecret("TestClient", "old-secret", "new-hashed-secret"))
            .Returns(false);

        // Act
        var result = _validator.GetStatus(client, "old-secret");

        // Assert
        Assert.That(result, Is.EqualTo(CurrentRegistrationStatus.DoesNotMatchSecret));
        
        // Verify previous secret was not checked
        _mockHashValidationCache.Verify(
            x => x.ValidateClientSecret("TestClient:previous", It.IsAny<string>(), It.IsAny<string>()), 
            Times.Never);
    }

    [Test]
    public void GetStatus_WithListOfClients_ShouldValidateFirstClient()
    {
        // Arrange
        var clients = new List<SettingClientBusinessEntity>
        {
            new()
            {
                Name = "TestClient",
                ClientSecret = "hashed-secret"
            },
            new()
            {
                Name = "TestClient",
                Instance = "Instance1",
                ClientSecret = "hashed-secret"
            }
        };
        _mockHashValidationCache
            .Setup(x => x.ValidateClientSecret("TestClient", "correct-secret", "hashed-secret"))
            .Returns(true);

        // Act
        var result = _validator.GetStatus(clients, "correct-secret");

        // Assert
        Assert.That(result, Is.EqualTo(CurrentRegistrationStatus.MatchesExistingSecret));
    }

    [Test]
    public void GetStatus_WithClientBase_ShouldUseCache()
    {
        // Arrange
        var client = new ClientStatusBusinessEntity
        {
            Name = "StatusClient",
            ClientSecret = "hashed-secret"
        };
        _mockHashValidationCache
            .Setup(x => x.ValidateClientSecret("StatusClient", "correct-secret", "hashed-secret"))
            .Returns(true);

        // Act
        var result = _validator.GetStatus(client, "correct-secret");

        // Assert
        Assert.That(result, Is.EqualTo(CurrentRegistrationStatus.MatchesExistingSecret));
        _mockHashValidationCache.Verify(
            x => x.ValidateClientSecret("StatusClient", "correct-secret", "hashed-secret"), 
            Times.Once);
    }
}
