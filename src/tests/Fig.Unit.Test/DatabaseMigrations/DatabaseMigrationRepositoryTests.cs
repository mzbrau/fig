using Fig.Api;
using Fig.Api.DatabaseMigrations;
using Fig.Api.Datalayer;
using Fig.Api.Datalayer.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NHibernate;
using NUnit.Framework;
using System.Data.Common;
using System.Threading.Tasks;

namespace Fig.Unit.Test.DatabaseMigrations;

[TestFixture]
public class DatabaseMigrationRepositoryTests
{
    private Mock<ISession> _mockSession = null!;
    private Mock<ISessionFactory> _mockSessionFactory = null!;
    private Mock<DbConnection> _mockConnection = null!;
    private Mock<ILogger<DatabaseMigrationRepository>> _mockLogger = null!;
    private IOptions<ApiSettings> _settings = null!;
    private DatabaseMigrationRepository _repository = null!;

    [SetUp]
    public void SetUp()
    {
        _mockSession = new Mock<ISession>();
        _mockSessionFactory = new Mock<ISessionFactory>();
        _mockConnection = new Mock<DbConnection>();
        _mockLogger = new Mock<ILogger<DatabaseMigrationRepository>>();
        _settings = Options.Create(new ApiSettings
        {
            DbConnectionString = "Data Source=fig.db"
        });
        
        _mockSession.Setup(s => s.Connection).Returns(_mockConnection.Object);
        
        _repository = new DatabaseMigrationRepository(
            _mockSession.Object,
            _mockSessionFactory.Object,
            _settings,
            _mockLogger.Object);
    }

    [Test]
    public async Task GetScriptForDatabase_ShouldReturnSqlServerScript_WhenConnectionIsSqlServer()
    {
        // Arrange
        _mockConnection.Setup(c => c.ConnectionString).Returns("Server=localhost;Database=TestDb;");
        
        var migration = new TestMigration();

        // Act
        var result = await _repository.GetScriptForDatabase(migration);

        // Assert
        Assert.That(result, Is.EqualTo("SQL Server Script"));
    }

    [Test]
    public async Task GetScriptForDatabase_ShouldReturnSqliteScript_WhenConnectionIsSqlite()
    {
        // Arrange
        _mockConnection.Setup(c => c.ConnectionString).Returns("Data Source=test.sqlite");
        
        var migration = new TestMigration();

        // Act
        var result = await _repository.GetScriptForDatabase(migration);

        // Assert
        Assert.That(result, Is.EqualTo("SQLite Script"));
    }

    [Test]
    public async Task GetScriptForDatabase_ShouldReturnSqliteScript_WhenConnectionIsInMemory()
    {
        // Arrange
        _mockConnection.Setup(c => c.ConnectionString).Returns("Data Source=:memory:");
        
        var migration = new TestMigration();

        // Act
        var result = await _repository.GetScriptForDatabase(migration);

        // Assert
        Assert.That(result, Is.EqualTo("SQLite Script"));
    }

    [Test]
    public async Task GetScriptForDatabase_ShouldReturnPostgresScript_WhenConnectionIsPostgres()
    {
        _mockConnection.Setup(c => c.ConnectionString)
            .Returns("Host=localhost;Port=5432;Database=fig;Username=fig;Password=x");

        var migration = new TestMigration();

        var result = await _repository.GetScriptForDatabase(migration);

        Assert.That(result, Is.EqualTo("Postgres Script"));
    }

    [Test]
    public async Task GetScriptForDatabase_ShouldHonorExplicitDatabaseProviderOverride()
    {
        _settings.Value.DatabaseProvider = DatabaseProvider.PostgreSql;
        _mockConnection.Setup(c => c.ConnectionString).Returns("Server=localhost;Database=TestDb;");

        var migration = new TestMigration();

        var result = await _repository.GetScriptForDatabase(migration);

        Assert.That(result, Is.EqualTo("Postgres Script"));
    }

    private class TestMigration : IDatabaseMigration
    {
        public int ExecutionNumber => 1;
        public string Description => "Test Migration";
        public string SqlServerScript => "SQL Server Script";
        public string SqliteScript => "SQLite Script";
        public string PostgresScript => "Postgres Script";
    }
}
