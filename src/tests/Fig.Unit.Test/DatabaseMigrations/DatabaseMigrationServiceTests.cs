using Fig.Api.DatabaseMigrations;
using Fig.Api.Services;
using Fig.Api.Datalayer.Repositories;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
using System;
using System.Threading.Tasks;

namespace Fig.Unit.Test.DatabaseMigrations;

[TestFixture]
public class DatabaseMigrationServiceTests
{
    private Mock<IDatabaseMigrationRepository> _mockRepository = null!;
    private Mock<ILogger<DatabaseMigrationService>> _mockLogger = null!;
    private Mock<IServiceProvider> _mockServiceProvider = null!;
    private Mock<IServiceScopeFactory> _mockServiceScopeFactory = null!;
    private Mock<IServiceScope> _mockServiceScope = null!;
    private DatabaseMigrationService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _mockRepository = new Mock<IDatabaseMigrationRepository>();
        _mockLogger = new Mock<ILogger<DatabaseMigrationService>>();
        _mockServiceProvider = new Mock<IServiceProvider>();
        _mockServiceScopeFactory = new Mock<IServiceScopeFactory>();
        _mockServiceScope = new Mock<IServiceScope>();

        // Setup service scope chain
        _mockServiceScope.Setup(s => s.ServiceProvider).Returns(_mockServiceProvider.Object);
        _mockServiceScopeFactory.Setup(f => f.CreateScope()).Returns(_mockServiceScope.Object);
        _mockServiceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(_mockServiceScopeFactory.Object);
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

        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object, _mockServiceProvider.Object);

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
            new DatabaseMigrationBusinessEntity { ExecutionNumber = 1, Status = "complete" }
        };

        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(executedMigrations);
        _mockRepository.Setup(r => r.TryBeginMigration(It.IsAny<int>(), It.IsAny<string>()))
            .ReturnsAsync((int num, string desc) => num == 2);
        _mockRepository.Setup(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()))
            .ReturnsAsync((IDatabaseMigration m) => $"SQL for {m.Description}");
        _mockRepository.Setup(r => r.CompleteMigration(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object, _mockServiceProvider.Object);

        // Act
        await _service.RunMigrationsAsync();

        // Assert
        _mockRepository.Verify(r => r.ExecuteRawSql("SQL for Second"), Times.Once);
        _mockRepository.Verify(r => r.ExecuteRawSql("SQL for First"), Times.Never);
        _mockRepository.Verify(r => r.CompleteMigration(2, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Test]
    public async Task RunMigrationsAsync_ShouldNotRunMigrations_WhenAnotherInstanceHandling()
    {
        // Arrange
        var migrations = new IDatabaseMigration[]
        {
            new TestMigration(1, "First")
        };

        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(new DatabaseMigrationBusinessEntity[0]);
        _mockRepository.Setup(r => r.TryBeginMigration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(false);

        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object, _mockServiceProvider.Object);

        // Act
        await _service.RunMigrationsAsync();

        // Assert
        _mockRepository.Verify(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()), Times.Never);
        _mockRepository.Verify(r => r.ExecuteRawSql(It.IsAny<string>()), Times.Never);
        _mockRepository.Verify(r => r.CompleteMigration(It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Never);
    }

    [Test]
    public void RunMigrationsAsync_ShouldThrowException_WhenMigrationFails()
    {
        // Arrange
        var migrations = new IDatabaseMigration[]
        {
            new TestMigration(1, "First")
        };

        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(new DatabaseMigrationBusinessEntity[0]);
        _mockRepository.Setup(r => r.TryBeginMigration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()))
            .ReturnsAsync("SQL for First");
        _mockRepository.Setup(r => r.ExecuteRawSql("SQL for First")).ThrowsAsync(new Exception("Database error"));
        _mockRepository.Setup(r => r.FailMigration(It.IsAny<int>())).Returns(Task.CompletedTask);

        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object, _mockServiceProvider.Object);

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

        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(new DatabaseMigrationBusinessEntity[0]);
        _mockRepository.Setup(r => r.TryBeginMigration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()))
            .ReturnsAsync(""); // Empty script
        _mockRepository.Setup(r => r.CompleteMigration(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object, _mockServiceProvider.Object);

        // Act
        await _service.RunMigrationsAsync();

        // Assert
        _mockRepository.Verify(r => r.ExecuteRawSql(It.IsAny<string>()), Times.Never);
        _mockRepository.Verify(r => r.CompleteMigration(It.IsAny<int>(), It.IsAny<TimeSpan>()), Times.Once);
    }

    [Test]
    public async Task RunMigrationsAsync_ShouldExecuteCodeMigration_WhenMigrationHasCodeExecution()
    {
        // Arrange
        var codeExecuted = false;
        var migrations = new IDatabaseMigration[]
        {
            new TestMigrationWithCode(1, "Code Migration", () => { codeExecuted = true; return Task.CompletedTask; })
        };

        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(new DatabaseMigrationBusinessEntity[0]);
        _mockRepository.Setup(r => r.TryBeginMigration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()))
            .ReturnsAsync(""); // Empty script - only code execution
        _mockRepository.Setup(r => r.CompleteMigration(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object, _mockServiceProvider.Object);

        // Act
        await _service.RunMigrationsAsync();

        // Assert
        Assert.That(codeExecuted, Is.True, "Code execution should have been called");
        _mockRepository.Verify(r => r.ExecuteRawSql(It.IsAny<string>()), Times.Never);
        _mockRepository.Verify(r => r.CompleteMigration(1, It.IsAny<TimeSpan>()), Times.Once);
    }

    [Test]
    public async Task RunMigrationsAsync_ShouldExecuteCodeThenSql_WhenMigrationHasBoth()
    {
        // Arrange
        var codeExecuted = false;
        var migrations = new IDatabaseMigration[]
        {
            new TestMigrationWithCode(1, "Mixed Migration", () => { codeExecuted = true; return Task.CompletedTask; })
        };

        _mockRepository.Setup(r => r.GetExecutedMigrations()).ReturnsAsync(new DatabaseMigrationBusinessEntity[0]);
        _mockRepository.Setup(r => r.TryBeginMigration(It.IsAny<int>(), It.IsAny<string>())).ReturnsAsync(true);
        _mockRepository.Setup(r => r.GetScriptForDatabase(It.IsAny<IDatabaseMigration>()))
            .ReturnsAsync("SQL for Mixed Migration");
        _mockRepository.Setup(r => r.CompleteMigration(It.IsAny<int>(), It.IsAny<TimeSpan>()))
            .Returns(Task.CompletedTask);

        _service = new DatabaseMigrationService(_mockRepository.Object, migrations, _mockLogger.Object, _mockServiceProvider.Object);

        // Act
        await _service.RunMigrationsAsync();

        // Assert
        Assert.That(codeExecuted, Is.True, "Code execution should have been called first");
        _mockRepository.Verify(r => r.ExecuteRawSql("SQL for Mixed Migration"), Times.Once);
        _mockRepository.Verify(r => r.CompleteMigration(1, It.IsAny<TimeSpan>()), Times.Once);
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

    private class TestMigrationWithCode : IDatabaseMigration
    {
        private readonly Func<Task> _codeToExecute;

        public int ExecutionNumber { get; }
        public string Description { get; }
        public string SqlServerScript => $"SQL Server SQL for {Description}";
        public string SqliteScript => $"SQLite SQL for {Description}";

        public TestMigrationWithCode(int executionNumber, string description, Func<Task> codeToExecute)
        {
            ExecutionNumber = executionNumber;
            Description = description;
            _codeToExecute = codeToExecute;
        }

        public Task? ExecuteCode(IServiceProvider serviceProvider) => _codeToExecute();
    }
}