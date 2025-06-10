using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class SettingClientDescriptionMap : ClassMapping<SettingClientDescriptionBusinessEntity>
{
    public SettingClientDescriptionMap()
    {
        Table(Mapping.SettingClientsTable);
        Id(x => x.Id, m => m.Generator(Generators.Assigned));
        Property(x => x.Description, x =>
        {
            x.Column("description");
            x.Type(NHibernateUtil.StringClob);
        });
        OneToOne(x => x.Client, m => m.Constrained(true));
    }
}