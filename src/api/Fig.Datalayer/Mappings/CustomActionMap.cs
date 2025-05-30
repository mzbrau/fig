using Fig.Datalayer.BusinessEntities.CustomActions;
using Fig.Datalayer.Constants;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings
{
    public class CustomActionMap : ClassMapping<CustomActionBusinessEntity>
    {
        public CustomActionMap()
        {
            Table(Mapping.CustomActionsTable);
            Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            Property(x => x.Name, x => x.Column("name"));
            Property(x => x.ButtonName, x => x.Column("button_name"));
            Property(x => x.Description, x => x.Column("description"));
            Property(x => x.SettingsUsed, x => x.Column("settings_used"));
        }
    }
}
