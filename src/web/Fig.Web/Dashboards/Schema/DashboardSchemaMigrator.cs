using Fig.Contracts.Dashboards;

namespace Fig.Web.Dashboards.Schema;

/// <summary>
/// Migrates persisted dashboard definitions forward when <see cref="DashboardDefinitionDataContract.SchemaVersion"/> increases.
/// v1 is a no-op identity migrator reserved for future schema changes.
/// </summary>
public interface IDashboardSchemaMigrator
{
    DashboardDefinitionDataContract Migrate(DashboardDefinitionDataContract definition);
}

public class DashboardSchemaMigrator : IDashboardSchemaMigrator
{
    public const int CurrentSchemaVersion = 1;

    public DashboardDefinitionDataContract Migrate(DashboardDefinitionDataContract definition)
    {
        ArgumentNullException.ThrowIfNull(definition);

        if (definition.SchemaVersion <= 0)
            definition.SchemaVersion = CurrentSchemaVersion;

        // Future migrations: if (definition.SchemaVersion < 2) { ...; definition.SchemaVersion = 2; }
        return definition;
    }
}
