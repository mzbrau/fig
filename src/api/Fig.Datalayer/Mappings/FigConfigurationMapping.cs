using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class FigConfigurationMapping : ClassMapping<FigConfigurationBusinessEntity>
{
    public FigConfigurationMapping()
    {
        Table("configuration");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.AllowNewRegistrations, x => x.Column("allow_new_registrations"));
        Property(x => x.AllowUpdatedRegistrations, x => x.Column("allow_updated_registrations"));
        Property(x => x.AllowFileImports, x => x.Column("allow_file_imports"));
        Property(x => x.AllowOfflineSettings, x => x.Column("allow_offline_settings"));
        Property(x => x.AllowDynamicVerifications, x => x.Column("allow_dynamic_verifications"));
        Property(x => x.DelayBeforeMemoryLeakMeasurementsMs, x => x.Column("delay_before_memory_leak_measurement_ms"));
        Property(x => x.IntervalBetweenMemoryLeakChecksMs, x => x.Column("interval_between_memory_leak_checks_ms"));
        Property(x => x.WebApplicationBaseAddress, x => x.Column("web_application_base_address"));
    }
}