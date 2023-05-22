using Fig.Common.NetStandard.WebHook;

namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class WebHookBusinessEntity
{
    public virtual Guid? Id { get; set; }
    
    public virtual WebHookType WebHookType { get; set; }
    
    public virtual string? ClientNameRegex { get; set; }
    
    public virtual string? SettingNameRegex { get; set; }
    
    public virtual int MinSessions { get; set; }
}