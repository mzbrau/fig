using Fig.Datalayer.BusinessEntities.CustomActions;
using FluentNHibernate.Mapping;

namespace Fig.Datalayer.Mappings.CustomActions
{
    public class CustomActionExecutionMap : ClassMap<CustomActionExecutionBusinessEntity>
    {
        public CustomActionExecutionMap()
        {
            Table("CustomActionExecutions");
            Id(x => x.Id).GeneratedBy.Guid();
            Map(x => x.CustomActionId);
            Map(x => x.Instance).Nullable();
            Map(x => x.SettingsJson).Length(4001).Nullable(); // Increased length for JSON
            Map(x => x.RequestedAt);
            Map(x => x.ExecutedAt).Nullable();
            Map(x => x.CompletedAt).Nullable();
            Map(x => x.Status);
            Map(x => x.ErrorMessage).Nullable();
            References(x => x.CustomAction).Column("CustomActionId").ReadOnly(); // ReadOnly as CustomActionId is explicitly mapped
            HasMany(x => x.Results)
                .KeyColumn("CustomActionExecutionId")
                .Inverse()
                .Cascade.AllDeleteOrphan();
        }
    }
}
