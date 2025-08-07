using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class DatabaseMigrationMap : ClassMapping<DatabaseMigrationBusinessEntity>
{
    public DatabaseMigrationMap()
    {
        Table(Mapping.DatabaseMigrationsTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ExecutionNumber, x => x.Column("execution_number"));
        Property(x => x.Description, x =>
        {
            x.Length(5012);
            x.Column("description");
        });
        Property(x => x.ExecutedAt, x => 
        {
            x.Column("executed_at");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.ExecutionDuration, x => 
        {
            x.Column("execution_duration_ticks");
            x.Type(NHibernateUtil.TimeSpan);
        });
        Property(x => x.Status, x =>
        {
            x.Column("status");
            x.Length(32);
        });
    }
}
