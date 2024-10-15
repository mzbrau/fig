using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class CheckPointDataMap : ClassMapping<CheckPointDataBusinessEntity>
{
    public CheckPointDataMap()
    {
        Table(Mapping.CheckPointTable);
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ExportAsJson, x =>
        {
            x.Type(NHibernateUtil.StringClob);
            x.Column("data");
        });
    }
}