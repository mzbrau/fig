using Fig.Datalayer.BusinessEntities.CustomActions;
using FluentNHibernate.Mapping;

namespace Fig.Datalayer.Mappings.CustomActions
{
    public class CustomActionMap : ClassMap<CustomActionBusinessEntity>
    {
        public CustomActionMap()
        {
            Table("CustomActions");
            Id(x => x.Id).GeneratedBy.Guid();
            Map(x => x.Name);
            Map(x => x.ButtonName);
            Map(x => x.Description);
            Map(x => x.SettingsUsedJson).Nullable();
            Map(x => x.SettingClientId);
            References(x => x.SettingClient).Column("SettingClientId").ReadOnly(); // ReadOnly as SettingClientId is explicitly mapped
            HasMany(x => x.Executions)
                .KeyColumn("CustomActionId")
                .Inverse()
                .Cascade.AllDeleteOrphan();
        }
    }
}
