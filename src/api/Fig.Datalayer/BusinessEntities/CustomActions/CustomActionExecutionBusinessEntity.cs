using System;
using System.Collections.Generic;

namespace Fig.Datalayer.BusinessEntities.CustomActions
{
    public class CustomActionExecutionBusinessEntity
    {
        public virtual Guid Id { get; set; }
        public virtual Guid CustomActionId { get; set; }
        public virtual string? Instance { get; set; }
        public virtual string? SettingsJson { get; set; }
        public virtual DateTime RequestedAt { get; set; }
        public virtual DateTime? ExecutedAt { get; set; }
        public virtual DateTime? CompletedAt { get; set; }
        public virtual string Status { get; set; }
        public virtual string? ErrorMessage { get; set; }
        public virtual CustomActionBusinessEntity CustomAction { get; set; }
        public virtual IList<CustomActionExecutionResultBusinessEntity> Results { get; set; } = new List<CustomActionExecutionResultBusinessEntity>();
    }
}
