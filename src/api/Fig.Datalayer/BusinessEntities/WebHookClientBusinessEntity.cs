namespace Fig.Datalayer.BusinessEntities;

public partial class WebHookClientBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual string Name { get; set; }
    
    public virtual string BaseUri { get; set; }
    
    public virtual string Secret { get; set; }
    
    public virtual string HashedSecret { get; set; }
}