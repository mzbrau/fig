using System;
using System.Collections.Generic;
using Fig.Api.Comparers;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ClientComparerTests
{
    private ClientComparer _comparer = null!;

    [SetUp]
    public void SetUp()
    {
        _comparer = new ClientComparer();
    }

    [Test]
    public void Equals_SameReference_ShouldReturnTrue()
    {
        // Arrange
        var client = CreateClient("TestClient", "Description");

        // Act
        var result = _comparer.Equals(client, client);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_BothNull_ShouldReturnTrue()
    {
        // Act
        var result = _comparer.Equals(null, null);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_FirstNull_ShouldReturnFalse()
    {
        // Arrange
        var client = CreateClient("TestClient", "Description");

        // Act
        var result = _comparer.Equals(null, client);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_SecondNull_ShouldReturnFalse()
    {
        // Arrange
        var client = CreateClient("TestClient", "Description");

        // Act
        var result = _comparer.Equals(client, null);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_IdenticalClients_ShouldReturnTrue()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));
        client1.Settings.Add(CreateSetting("Setting2", "Description2"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));
        client2.Settings.Add(CreateSetting("Setting2", "Description2"));

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_DifferentNames_ShouldReturnFalse()
    {
        // Arrange
        var client1 = CreateClient("TestClient1", "Description");
        var client2 = CreateClient("TestClient2", "Description");

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_DifferentDescriptions_ShouldReturnFalse()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description1");
        var client2 = CreateClient("TestClient", "Description2");

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_DifferentSettingCounts_ShouldReturnFalse()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));
        client1.Settings.Add(CreateSetting("Setting2", "Description2"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_SettingsRemoved_ShouldReturnFalse()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));
        client1.Settings.Add(CreateSetting("Setting2", "Description2"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));
        client2.Settings.Add(CreateSetting("Setting3", "Description3"));

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_SettingsAdded_ShouldReturnFalse()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));
        client2.Settings.Add(CreateSetting("Setting2", "Description2"));

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_SettingsInDifferentOrder_ShouldReturnTrue()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));
        client1.Settings.Add(CreateSetting("Setting2", "Description2"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Settings.Add(CreateSetting("Setting2", "Description2"));
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_DifferentSettingProperties_ShouldReturnFalse()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));

        var client2 = CreateClient("TestClient", "Description");
        var setting = CreateSetting("Setting1", "Description1");
        setting.IsSecret = true;
        client2.Settings.Add(setting);

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_DifferentInstances_ShouldReturnTrue()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Instance = "Instance1";
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Instance = "Instance2";
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert - Instance property should be ignored
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_OneInstanceNullOtherNot_ShouldReturnTrue()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Instance = null;
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Instance = "Instance1";
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert - Instance property should be ignored
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_DifferentClientSecrets_ShouldReturnTrue()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.ClientSecret = "Secret1";

        var client2 = CreateClient("TestClient", "Description");
        client2.ClientSecret = "Secret2";

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert - ClientSecret should be ignored
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_DifferentIds_ShouldReturnTrue()
    {
        // Arrange
        var client1 = new SettingClientBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestClient",
            Description = "Description"
        };

        var client2 = new SettingClientBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestClient",
            Description = "Description"
        };

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert - Id should be ignored
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_EmptySettings_ShouldReturnTrue()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        var client2 = CreateClient("TestClient", "Description");

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void GetHashCode_IdenticalClients_ShouldReturnSameHashCode()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));

        // Act
        var hash1 = _comparer.GetHashCode(client1);
        var hash2 = _comparer.GetHashCode(client2);

        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
    }

    [Test]
    public void GetHashCode_DifferentNames_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var client1 = CreateClient("TestClient1", "Description");
        var client2 = CreateClient("TestClient2", "Description");

        // Act
        var hash1 = _comparer.GetHashCode(client1);
        var hash2 = _comparer.GetHashCode(client2);

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void GetHashCode_DifferentDescriptions_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description1");
        var client2 = CreateClient("TestClient", "Description2");

        // Act
        var hash1 = _comparer.GetHashCode(client1);
        var hash2 = _comparer.GetHashCode(client2);

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void GetHashCode_DifferentSettings_ShouldReturnDifferentHashCodes()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Settings.Add(CreateSetting("Setting2", "Description2"));

        // Act
        var hash1 = _comparer.GetHashCode(client1);
        var hash2 = _comparer.GetHashCode(client2);

        // Assert
        Assert.That(hash1, Is.Not.EqualTo(hash2));
    }

    [Test]
    public void GetHashCode_ConsistentAcrossMultipleCalls_ShouldReturnSameValue()
    {
        // Arrange
        var client = CreateClient("TestClient", "Description");
        client.Settings.Add(CreateSetting("Setting1", "Description1"));

        // Act
        var hash1 = _comparer.GetHashCode(client);
        var hash2 = _comparer.GetHashCode(client);
        var hash3 = _comparer.GetHashCode(client);

        // Assert
        Assert.That(hash1, Is.EqualTo(hash2));
        Assert.That(hash2, Is.EqualTo(hash3));
    }

    [Test]
    public void Equals_ComplexSettingsWithMultipleProperties_ShouldCompareCorrectly()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        var setting1 = CreateSetting("Setting1", "Description1");
        setting1.IsSecret = true;
        setting1.ValidationRegex = "^[a-z]+$";
        setting1.ValidationExplanation = "Must be lowercase";
        setting1.Group = "Group1";
        setting1.DisplayOrder = 1;
        setting1.Advanced = true;
        client1.Settings.Add(setting1);

        var client2 = CreateClient("TestClient", "Description");
        var setting2 = CreateSetting("Setting1", "Description1");
        setting2.IsSecret = true;
        setting2.ValidationRegex = "^[a-z]+$";
        setting2.ValidationExplanation = "Must be lowercase";
        setting2.Group = "Group1";
        setting2.DisplayOrder = 1;
        setting2.Advanced = true;
        client2.Settings.Add(setting2);

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_ComplexSettingsWithDifferentValidationRegex_ShouldReturnFalse()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        var setting1 = CreateSetting("Setting1", "Description1");
        setting1.ValidationRegex = "^[a-z]+$";
        client1.Settings.Add(setting1);

        var client2 = CreateClient("TestClient", "Description");
        var setting2 = CreateSetting("Setting1", "Description1");
        setting2.ValidationRegex = "^[A-Z]+$";
        client2.Settings.Add(setting2);

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_MultipleSettingsOneModified_ShouldReturnFalse()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.Settings.Add(CreateSetting("Setting1", "Description1"));
        client1.Settings.Add(CreateSetting("Setting2", "Description2"));
        client1.Settings.Add(CreateSetting("Setting3", "Description3"));

        var client2 = CreateClient("TestClient", "Description");
        client2.Settings.Add(CreateSetting("Setting1", "Description1"));
        var modifiedSetting = CreateSetting("Setting2", "Description2");
        modifiedSetting.Group = "ModifiedGroup";
        client2.Settings.Add(modifiedSetting);
        client2.Settings.Add(CreateSetting("Setting3", "Description3"));

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert
        Assert.That(result, Is.False);
    }

    [Test]
    public void Equals_WithCustomActions_ShouldIgnoreCustomActions()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.CustomActions.Add(new CustomActionBusinessEntity { Name = "Action1" });

        var client2 = CreateClient("TestClient", "Description");
        client2.CustomActions.Add(new CustomActionBusinessEntity { Name = "Action2" });

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert - CustomActions should be ignored
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_WithDifferentLastRegistrationDates_ShouldIgnoreTimestamps()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.LastRegistration = DateTime.UtcNow;
        client1.LastSettingValueUpdate = DateTime.UtcNow.AddMinutes(-5);

        var client2 = CreateClient("TestClient", "Description");
        client2.LastRegistration = DateTime.UtcNow.AddHours(-1);
        client2.LastSettingValueUpdate = DateTime.UtcNow.AddMinutes(-30);

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert - Timestamps should be ignored
        Assert.That(result, Is.True);
    }

    [Test]
    public void Equals_WithRunSessions_ShouldIgnoreRunSessions()
    {
        // Arrange
        var client1 = CreateClient("TestClient", "Description");
        client1.RunSessions.Add(new ClientRunSessionBusinessEntity());

        var client2 = CreateClient("TestClient", "Description");
        // client2 has no run sessions

        // Act
        var result = _comparer.Equals(client1, client2);

        // Assert - RunSessions should be ignored
        Assert.That(result, Is.True);
    }

    private static SettingClientBusinessEntity CreateClient(string name, string description)
    {
        return new SettingClientBusinessEntity
        {
            Name = name,
            Description = description,
            Settings = new List<SettingBusinessEntity>()
        };
    }

    private static SettingBusinessEntity CreateSetting(string name, string description)
    {
        return new SettingBusinessEntity
        {
            Name = name,
            Description = description,
            IsSecret = false,
            ValueType = typeof(string),
            DefaultValue = new StringSettingBusinessEntity("default")
        };
    }
}
