using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings;

public class VerificationResultMap : ClassMapping<VerificationResultBusinessEntity>
{
    public VerificationResultMap()
    {
        Table("verification_result_history");
        Id(x => x.Id, m => m.Generator(Generators.GuidComb));
        Property(x => x.ClientId, x => x.Column("client_id"));
        Property(x => x.VerificationName, x => x.Column("verification_name"));
        Property(x => x.Success, x => x.Column("success"));
        Property(x => x.Message, x => x.Column("message"));
        Property(x => x.RequestingUser, x => x.Column("requesting_user"));
        Property(x => x.ExecutionTime, x =>
        {
            x.Column("execution_time");
            x.Type(NHibernateUtil.UtcTicks);
        });
        Property(x => x.LogsAsJson, x =>
        {
            x.Column("logs_as_json");
            x.Type(NHibernateUtil.StringClob);
        });
    }
}