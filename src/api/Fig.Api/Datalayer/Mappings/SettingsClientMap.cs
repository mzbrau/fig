using Fig.Api.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Api.Datalayer.Mappings;

public class SettingsClientMap : ClassMapping<SettingsClientBusinessEntity>
{
    public SettingsClientMap()
    {
        Table("setting_client");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name);
        Property(x => x.Instance);
        Property(x => x.ClientSecret);
    }
}