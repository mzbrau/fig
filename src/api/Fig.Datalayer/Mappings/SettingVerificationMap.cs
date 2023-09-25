using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class SettingVerificationMap : ClassMapping<SettingVerificationBusinessEntity>
{
    public SettingVerificationMap()
    {
        Table(Mapping.SettingVerificationsTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.Name, x => x.Column("name"));
        Property(x => x.PropertyArgumentsAsJson, x =>
        {
            x.Column("property_arguments");
            x.Type(NHibernateUtil.StringClob);
        });
    }
}