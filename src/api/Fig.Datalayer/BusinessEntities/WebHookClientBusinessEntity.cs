namespace Fig.Datalayer.BusinessEntities;

// ReSharper disable once ClassWithVirtualMembersNeverInherited.Global required by nhibernate.
public class WebHookClientBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual string Name { get; set; }
    
    public virtual string BaseUri { get; set; }
    
    public virtual string Secret { get; set; }
}