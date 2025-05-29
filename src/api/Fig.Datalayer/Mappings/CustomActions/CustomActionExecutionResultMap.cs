using Fig.Datalayer.BusinessEntities.CustomActions;
using FluentNHibernate.Mapping;

namespace Fig.Datalayer.Mappings.CustomActions
{
    public class CustomActionExecutionResultMap : ClassMap<CustomActionExecutionResultBusinessEntity>
    {
        public CustomActionExecutionResultMap()
        {
            Table("CustomActionExecutionResults");
            Id(x => x.Id).GeneratedBy.Guid();
            Map(x => x.CustomActionExecutionId);
            Map(x => x.Name);
            Map(x => x.ResultType);
            Map(x => x.TextResult).Nullable();
            Map(x => x.DataGridResultJson).Length(4001).Nullable(); // Increased length for JSON
            References(x => x.Execution).Column("CustomActionExecutionId").ReadOnly(); // ReadOnly as CustomActionExecutionId is explicitly mapped
        }
    }
}
