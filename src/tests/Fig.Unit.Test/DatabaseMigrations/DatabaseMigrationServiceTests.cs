using Fig.Api.DatabaseMigrations;
using Fig.Api.Services;
using Fig.Api.Datalayer.Repositories;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Fig.Unit.Test.DatabaseMigrations;

[TestFixture]
public class DatabaseMigrationServiceTests
{
    private Mock<IDatabaseMigrationRepository> _mockRepository = null!;
    private Mock<ILogger<DatabaseMigrationService>> _mockLogger = null!;
    private DatabaseMigrationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IDatabaseMigrationRepository>();
        _mockLogger = new Mock<ILogger<DatabaseMigrationService>>();
    }

    [Test]
    public void RunMigrationsAsync_ShouldThrowException_WhenMigrationsHaveNonSequentialNumbers()
    {
        // Arrange
        var migrations = new IDatabaseMigration[]
        {
            new TestMigration(1, "First"),
            new TestMigration(3, "Third") // Missing migration 2
        };

        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RunMigrationsAsync());
        
        Assert.That(ex!.Message, Does.Contain("Expected migration number 2, but found 3"));
    }

    [Test]
    public async Task RunMigrationsAsync_ShouldSkipExecutedMigrations()
    {
        // Arrange
        var migrations = new IDatabaseMigration[]
        {
            new TestMigration(1, "First"),
            new TestMigration(2, "Second")
        };

        var executedMigrations = new[]
        {
            new DatabaseMigrationBusinessEntity { ExecutionNumber = 1 }
        };

        _mockRepository.Setup(r => r.TryAcquireMigrationLock(It.IsAny<int>())).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(executedMigrations);
        _mockRepository.Setup(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()))
            .ReturnsAsync((IDatabaseMigration m) => $"SQL for {m.Description}");
        
        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object);

        // Act
        await _service.RunMigrationsAsync();

        // Assert
        _mockRepository.Verify(r => r.ExecuteRawSql("SQL for Second"), Times.Once);
        _mockRepository.Verify(r => r.ExecuteRawSql("SQL for First"), Times.Never);
        _mockRepository.Verify(r => r.RecordMigrationExecution(It.IsAny<DatabaseMigrationBusinessEntity>()), Times.Once);
    }

    [Test]
    public async Task RunMigrationsAsync_ShouldNotRunMigrations_WhenLockNotAcquired()
    {
        // Arrange
        var migrations = new IDatabaseMigration[]
        {
            new TestMigration(1, "First")
        };

        _mockRepository.Setup(r => r.TryAcquireMigrationLock(It.IsAny<int>())).ReturnsAsync(false);
        
        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object);

        // Act
        await _service.RunMigrationsAsync();

        // Assert
        _mockRepository.Verify(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()), Times.Never);
        _mockRepository.Verify(r => r.ExecuteRawSql(It.IsAny<string>()), Times.Never);
        _mockRepository.Verify(r => r.RecordMigrationExecution(It.IsAny<DatabaseMigrationBusinessEntity>()), Times.Never);
    }

    [Test]
    public void RunMigrationsAsync_ShouldThrowException_WhenMigrationFails()
    {
        // Arrange
        var migrations = new IDatabaseMigration[]
        {
            new TestMigration(1, "First")
        };

        _mockRepository.Setup(r => r.TryAcquireMigrationLock(It.IsAny<int>())).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(new DatabaseMigrationBusinessEntity[0]);
        _mockRepository.Setup(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()))
            .ReturnsAsync("SQL for First");
        _mockRepository.Setup(r => r.ExecuteRawSql("SQL for First")).ThrowsAsync(new Exception("Database error"));
        
        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object);

        // Act & Assert
        var ex = Assert.ThrowsAsync<InvalidOperationException>(() => _service.RunMigrationsAsync());
        Assert.That(ex!.Message, Does.Contain("Migration 1 failed"));
    }

    [Test]
    public async Task RunMigrationsAsync_ShouldSkipMigration_WhenScriptIsEmpty()
    {
        // Arrange
        var migrations = new IDatabaseMigration[]
        {
            new TestMigration(1, "First")
        };

        _mockRepository.Setup(r => r.TryAcquireMigrationLock(It.IsAny<int>())).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(new DatabaseMigrationBusinessEntity[0]);
        _mockRepository.Setup(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()))
            .ReturnsAsync(""); // Empty script
        
        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object);

        // Act
        await _service.RunMigrationsAsync();

        // Assert
        _mockRepository.Verify(r => r.ExecuteRawSql(It.IsAny<string>()), Times.Never);
        _mockRepository.Verify(r => r.RecordMigrationExecution(It.IsAny<DatabaseMigrationBusinessEntity>()), Times.Never);
    }

    private class TestMigration : IDatabaseMigration
    {
        public int ExecutionNumber { get; }
        public string Description { get; }
        public string SqlServerScript => $"SQL Server SQL for {Description}";
        public string SqliteScript => $"SQLite SQL for {Description}";

        public TestMigration(int executionNumber, string description)
        {
            ExecutionNumber = executionNumber;
            Description = description;
        }
    }
}
