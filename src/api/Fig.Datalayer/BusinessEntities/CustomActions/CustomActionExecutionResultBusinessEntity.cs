using System;

namespace Fig.Datalayer.BusinessEntities.CustomActions
{
    public class CustomActionExecutionResultBusinessEntity
    {
        public virtual Guid Id { get; set; }
        public virtual Guid CustomActionExecutionId { get; set; }
        public virtual string Name { get; set; }
        public virtual string ResultType { get; set; } // Text, DataGrid
        public virtual string? TextResult { get; set; }
        public virtual string? DataGridResultJson { get; set; } // Serialized DataGridSettingDataContract
        public virtual CustomActionExecutionBusinessEntity Execution { get; set; }
    }
}
