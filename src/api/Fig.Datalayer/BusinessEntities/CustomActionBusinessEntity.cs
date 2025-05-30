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
        
        public virtual string SettingsUsed { get; set; }
        
        public virtual string ClientName { get; set; }
    }
}
