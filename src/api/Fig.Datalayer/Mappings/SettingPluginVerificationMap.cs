using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class SettingPluginVerificationMap : ClassMapping<SettingPluginVerificationBusinessEntity>
{
    public SettingPluginVerificationMap()
    {
        Table("setting_plugin_verification");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.PropertyArguments, x => x.Column("property_arguments"));

    }
}