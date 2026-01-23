using Fig.Api.DatabaseMigrations.Migrations;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Validators;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Fig.Unit.Test.DatabaseMigrations;

[TestFixture]
public class Migration_003_MigrateCodeHashesTests
{
    private Migration_003_MigrateCodeHashes _migration = null!;
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    private Mock<ILogger<Migration_003_MigrateCodeHashes>> _mockLogger = null!;
    private Mock<ISettingClientRepository> _mockRepository = null!;
    private Mock<ICodeHasher> _mockNewHasher = null!;
    private Mock<ILegacyCodeHasher> _mockLegacyHasher = null!;

    [SetUp]
    public void SetUp()
    {
        _migration = new Migration_003_MigrateCodeHashes();
        
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockLogger = new Mock<ILogger<Migration_003_MigrateCodeHashes>>();
        _mockRepository = new Mock<ISettingClientRepository>();
        _mockNewHasher = new Mock<ICodeHasher>();
        _mockLegacyHasher = new Mock<ILegacyCodeHasher>();

        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILogger<Migration_003_MigrateCodeHashes>)))
            .Returns(_mockLogger.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ISettingClientRepository)))
            .Returns(_mockRepository.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ICodeHasher)))
            .Returns(_mockNewHasher.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(ILegacyCodeHasher)))
            .Returns(_mockLegacyHasher.Object);
    }

    [Test]
    public void ExecutionNumber_ShouldReturn3()
    {
        // Assert
        Assert.That(_migration.ExecutionNumber, Is.EqualTo(3));
    }

    [Test]
    public void Scripts_ShouldBeEmpty()
    {
        // Assert
        Assert.That(_migration.SqlServerScript, Is.EqualTo(string.Empty));
        Assert.That(_migration.SqliteScript, Is.EqualTo(string.Empty));
    }

    [Test]
    public async Task ExecuteCode_ShouldMigrateLegacyHashes()
    {
        // Arrange
        const string displayScript = "console.log('test');";
        const string legacyHash = "$2a$11$oldHash";
        const string newHash = "newHashValue";

        var client = new SettingClientBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestClient",
            Settings = new List<SettingBusinessEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "TestSetting",
                    DisplayScript = displayScript,
                    DisplayScriptHash = legacyHash
                }
            }
        };

        _mockRepository.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), true, false))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { client });

        _mockLegacyHasher.Setup(h => h.IsValid(legacyHash, displayScript))
            .Returns(true);

        _mockNewHasher.Setup(h => h.GetHash(displayScript))
            .Returns(newHash);

        // Act
        var executeTask = _migration.ExecuteCode(_mockServiceProvider.Object);
        Assert.That(executeTask, Is.Not.Null);
        await executeTask!;

        // Assert
        Assert.That(client.Settings.First().DisplayScriptHash, Is.EqualTo(newHash));
        _mockRepository.Verify(r => r.UpdateClient(client), Times.Once);
    }

    [Test]
    public async Task ExecuteCode_ShouldNotUpdateAlreadyMigratedHashes()
    {
        // Arrange
        const string displayScript = "console.log('test');";
        const string newHash = "newHashValue";

        var client = new SettingClientBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestClient",
            Settings = new List<SettingBusinessEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "TestSetting",
                    DisplayScript = displayScript,
                    DisplayScriptHash = newHash
                }
            }
        };

        _mockRepository.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), true, false))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { client });

        _mockLegacyHasher.Setup(h => h.IsValid(newHash, displayScript))
            .Returns(false);

        _mockNewHasher.Setup(h => h.IsValid(newHash, displayScript))
            .Returns(true);

        // Act
        var executeTask = _migration.ExecuteCode(_mockServiceProvider.Object);
        Assert.That(executeTask, Is.Not.Null);
        await executeTask!;

        // Assert
        Assert.That(client.Settings.First().DisplayScriptHash, Is.EqualTo(newHash));
        _mockRepository.Verify(r => r.UpdateClient(It.IsAny<SettingClientBusinessEntity>()), Times.Never);
    }

    [Test]
    public async Task ExecuteCode_ShouldRemoveInvalidHashes()
    {
        // Arrange
        const string displayScript = "console.log('test');";
        const string invalidHash = "invalidHash";

        var client = new SettingClientBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestClient",
            Settings = new List<SettingBusinessEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "TestSetting",
                    DisplayScript = displayScript,
                    DisplayScriptHash = invalidHash
                }
            }
        };

        _mockRepository.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), true, false))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { client });

        _mockLegacyHasher.Setup(h => h.IsValid(invalidHash, displayScript))
            .Returns(false);

        _mockNewHasher.Setup(h => h.IsValid(invalidHash, displayScript))
            .Returns(false);

        // Act
        var executeTask = _migration.ExecuteCode(_mockServiceProvider.Object);
        Assert.That(executeTask, Is.Not.Null);
        await executeTask!;

        // Assert
        var setting = client.Settings.First();
        Assert.That(setting.DisplayScript, Is.Null);
        Assert.That(setting.DisplayScriptHash, Is.Null);
        _mockRepository.Verify(r => r.UpdateClient(client), Times.Once);
    }

    [Test]
    public async Task ExecuteCode_ShouldSkipSettingsWithoutHashOrScript()
    {
        // Arrange
        var client = new SettingClientBusinessEntity
        {
            Id = Guid.NewGuid(),
            Name = "TestClient",
            Settings = new List<SettingBusinessEntity>
            {
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "TestSetting1",
                    DisplayScript = null,
                    DisplayScriptHash = null
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "TestSetting2",
                    DisplayScript = "console.log('test');",
                    DisplayScriptHash = null
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Name = "TestSetting3",
                    DisplayScript = null,
                    DisplayScriptHash = "someHash"
                }
            }
        };

        _mockRepository.Setup(r => r.GetAllClients(It.IsAny<Fig.Contracts.Authentication.UserDataContract>(), true, false))
            .ReturnsAsync(new List<SettingClientBusinessEntity> { client });

        // Act
        var executeTask = _migration.ExecuteCode(_mockServiceProvider.Object);
        Assert.That(executeTask, Is.Not.Null);
        await executeTask!;

        // Assert
        _mockLegacyHasher.Verify(h => h.IsValid(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
        _mockNewHasher.Verify(h => h.GetHash(It.IsAny<string>()), Times.Never);
        _mockRepository.Verify(r => r.UpdateClient(It.IsAny<SettingClientBusinessEntity>()), Times.Never);
    }
}