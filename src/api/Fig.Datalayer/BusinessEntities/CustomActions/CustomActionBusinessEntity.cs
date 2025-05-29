using System;
using System.Collections.Generic;

namespace Fig.Datalayer.BusinessEntities.CustomActions
{
    public class CustomActionBusinessEntity
    {
        public virtual Guid Id { get; set; }
        public virtual string Name { get; set; }
        public virtual string ButtonName { get; set; }
        public virtual string Description { get; set; }
        public virtual string? SettingsUsedJson { get; set; }
        public virtual Guid SettingClientId { get; set; }
        public virtual SettingClientBusinessEntity SettingClient { get; set; }
        public virtual IList<CustomActionExecutionBusinessEntity> Executions { get; set; } = new List<CustomActionExecutionBusinessEntity>();
    }
}
