using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class RunSessionMemoryUsageMap : ClassMapping<MemoryUsageBusinessEntity>
{
    public RunSessionMemoryUsageMap()
    {
        Table(Mapping.RunSessionMemoryUsageTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ClientRunTimeSeconds, x => x.Column("client_runtime_seconds"));
        Property(x => x.MemoryUsageBytes, x => x.Column("memory_usage_bytes"));
    }
}