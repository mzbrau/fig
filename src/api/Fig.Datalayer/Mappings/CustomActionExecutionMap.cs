using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.Constants;
using NHibernate;
using NHibernate.Mapping.ByCode;
using NHibernate.Mapping.ByCode.Conformist;

namespace Fig.Datalayer.Mappings
{
    public class CustomActionExecutionMap : ClassMapping<CustomActionExecutionBusinessEntity>
    {
        public CustomActionExecutionMap()
        {
            Table(Mapping.CustomActionExecutionsTable);
            Id(x => x.Id, m => m.Generator(Generators.GuidComb));
            Property(x => x.ClientName, x => x.Column("client_name"));
            Property(x => x.CustomActionName, x => x.Column("custom_action_name"));
            Property(x => x.HandlingInstance, x => x.Column("handling_instance"));
            Property(x => x.Succeeded, x => x.Column("succeeded"));
            Property(x => x.RunSessionId, x => x.Column("run_session_id"));
            Property(x => x.ExecutedByRunSessionId, x => x.Column("executed_by_run_session_id"));
            Property(x => x.RequestedAt, x =>
            {
                x.Column("requested_at");
                x.Type(NHibernateUtil.UtcTicks);
            });
            Property(x => x.ExecutedAt, x =>
            {
                x.Column("executed_at");
                x.Type(NHibernateUtil.UtcTicks);
            });
            Property(x => x.ResultsAsJson, x =>
            {
                x.Column("results_as_json");
                x.Type(NHibernateUtil.StringClob);
            });
        }
    }
}
