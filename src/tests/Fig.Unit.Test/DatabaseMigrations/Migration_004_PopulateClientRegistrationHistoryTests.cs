using System;
using System.Collections.Generic;
using Fig.Api.DatabaseMigrations.Migrations;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using NUnit.Framework;

namespace Fig.Unit.Test.DatabaseMigrations;

[TestFixture]
public class Migration_004_PopulateClientRegistrationHistoryTests
{
    private Migration_004_PopulateClientRegistrationHistory _migration = null!;

    [SetUp]
    public void SetUp()
    {
        _migration = new Migration_004_PopulateClientRegistrationHistory();
    }

    [Test]
    public void ExecutionNumber_ShouldReturn4()
    {
        Assert.That(_migration.ExecutionNumber, Is.EqualTo(4));
    }

    [Test]
    public void Scripts_ShouldBeEmpty()
    {
        Assert.That(_migration.SqlServerScript, Is.EqualTo(string.Empty));
        Assert.That(_migration.SqliteScript, Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetDefaultValueAsString_ReturnsNull_WhenDefaultValueIsNull()
    {
        var setting = new SettingBusinessEntity { Name = "S", DefaultValue = null };

        Assert.That(Migration_004_PopulateClientRegistrationHistory.GetDefaultValueAsString(setting), Is.Null);
    }

    [Test]
    public void GetDefaultValueAsString_SerializesStringDefault()
    {
        var setting = new SettingBusinessEntity
        {
            Name = "S",
            DefaultValue = new StringSettingBusinessEntity("hello")
        };

        var result = Migration_004_PopulateClientRegistrationHistory.GetDefaultValueAsString(setting);

        Assert.That(result, Is.EqualTo("\"hello\""));
    }

    [Test]
    public void GetDefaultValueAsString_SerializesIntDefault()
    {
        var setting = new SettingBusinessEntity
        {
            Name = "S",
            DefaultValue = new IntSettingBusinessEntity(42)
        };

        var result = Migration_004_PopulateClientRegistrationHistory.GetDefaultValueAsString(setting);

        Assert.That(result, Is.EqualTo("42"));
    }

    [Test]
    public void GetClientVersion_ReturnsEmpty_WhenNoRunSessions()
    {
        var client = new SettingClientBusinessEntity
        {
            Name = "Client",
            RunSessions = new List<ClientRunSessionBusinessEntity>()
        };

        Assert.That(Migration_004_PopulateClientRegistrationHistory.GetClientVersion(client), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetClientVersion_ReturnsLatestSessionApplicationVersion()
    {
        var older = DateTime.UtcNow.AddHours(-2);
        var newer = DateTime.UtcNow.AddHours(-1);
        var client = new SettingClientBusinessEntity
        {
            Name = "Client",
            RunSessions =
            [
                new ClientRunSessionBusinessEntity { LastSeen = older, ApplicationVersion = "1.0.0" },
                new ClientRunSessionBusinessEntity { LastSeen = newer, ApplicationVersion = "2.0.0" }
            ]
        };

        Assert.That(Migration_004_PopulateClientRegistrationHistory.GetClientVersion(client), Is.EqualTo("2.0.0"));
    }
}
