using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using Fig.Api.DatabaseMigrations;
using Fig.Api.DatabaseMigrations.Migrations;

namespace Fig.Unit.Test.DatabaseMigrations;

[TestFixture]
public class MigrationDiscoveryTests
{
    [Test]
    public void Migration_002_ShouldBeDiscoverable()
    {
        // Arrange & Act
        var migration = new Migration_002_DisableTimeMachine();
        
        // Assert
        Assert.That(migration.ExecutionNumber, Is.EqualTo(2));
        Assert.That(migration.Description, Is.EqualTo("Disable time machine feature in configuration table"));
        Assert.That(migration.SqlServerScript, Is.Not.Empty);
        Assert.That(migration.SqliteScript, Is.Not.Empty);
    }
    
    [Test]
    public void Migration_002_SqlServerScript_ShouldUpdateEnableTimeMachine()
    {
        // Arrange
        var migration = new Migration_002_DisableTimeMachine();
        
        // Act
        var script = migration.SqlServerScript;
        
        // Assert
        Assert.That(script, Does.Contain("UPDATE configuration"));
        Assert.That(script, Does.Contain("enable_time_machine = 0"));
    }
    
    [Test]
    public void Migration_002_SqliteScript_ShouldUpdateEnableTimeMachine()
    {
        // Arrange
        var migration = new Migration_002_DisableTimeMachine();
        
        // Act
        var script = migration.SqliteScript;
        
        // Assert
        Assert.That(script, Does.Contain("UPDATE configuration"));
        Assert.That(script, Does.Contain("enable_time_machine = 0"));
    }
    
    [Test]
    public void AllMigrations_ShouldHaveSequentialNumbers()
    {
        // Arrange & Act
        var assembly = Assembly.Load("Fig.Api");
        var migrationType = typeof(IDatabaseMigration);
        var migrations = assembly.GetTypes()
            .Where(t => migrationType.IsAssignableFrom(t) && !t.IsInterface && !t.IsAbstract)
            .Select(t => Activator.CreateInstance(t) as IDatabaseMigration)
            .Where(m => m != null)
            .OrderBy(m => m?.ExecutionNumber ?? 0)
            .ToList();
        
        // Assert
        Assert.That(migrations.Count, Is.GreaterThanOrEqualTo(2), "Should have at least 2 migrations");
        
        for (int i = 0; i < migrations.Count; i++)
        {
            Assert.That(migrations[i]?.ExecutionNumber, Is.EqualTo(i + 1), 
                $"Migration at index {i} should have ExecutionNumber {i + 1}");
        }
    }
}
