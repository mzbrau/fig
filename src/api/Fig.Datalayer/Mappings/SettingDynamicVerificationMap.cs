using Fig.Datalayer.BusinessEntities;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class SettingDynamicVerificationMap : ClassMapping<SettingDynamicVerificationBusinessEntity>
{
    public SettingDynamicVerificationMap()
    {
        Table("setting_dynamic_verification");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.Description, x => x.Column("description"));
        Property(x => x.Code, x => x.Column("code"));
        Property(x => x.CodeHash, x => x.Column("code_hash"));
        Property(x => x.TargetRuntime, x => x.Column("target_runtime"));
        Property(x => x.SettingsVerifiedAsJson, x => x.Column("settings_verified"));
    }
}