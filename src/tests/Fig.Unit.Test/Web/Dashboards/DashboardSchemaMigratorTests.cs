using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Schema;
using NUnit.Framework;

namespace Fig.Unit.Test.Web.Dashboards;

[TestFixture]
public class DashboardSchemaMigratorTests
{
    [Test]
    public void ShallDefaultZeroSchemaVersionToV1()
    {
        var migrator = new DashboardSchemaMigrator();
        var definition = new DashboardDefinitionDataContract { SchemaVersion = 0 };

        var migrated = migrator.Migrate(definition);

        Assert.That(migrated.SchemaVersion, Is.EqualTo(1));
    }
}
