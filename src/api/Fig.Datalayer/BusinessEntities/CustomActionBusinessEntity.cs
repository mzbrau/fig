using System.Runtime.InteropServices;

namespace Fig.Datalayer.BusinessEntities
{
    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
    public class CustomActionBusinessEntity
    {
        public virtual Guid Id { get; init; }
        
        public virtual string Name { get; set; } = null!;
        
        public virtual string ButtonName { get; set; } = null!;
        
        public virtual string Description { get; set; } = null!;
        
        public virtual string SettingsUsed { get; set; } = null!;
        
        public virtual string ClientName { get; set; } = null!;
        
        public virtual Guid ClientReference { get; set; }
    }
}
