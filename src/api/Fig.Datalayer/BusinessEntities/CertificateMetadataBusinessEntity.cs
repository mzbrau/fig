namespace Fig.Datalayer.BusinessEntities;

public class CertificateMetadataBusinessEntity
{
    public virtual Guid Id { get; set; }
    
    public virtual string Thumbprint { get; set; }
    
    public virtual DateTime ValidFrom { get; set; }
    
    public virtual DateTime ValidTo { get; set; }
    
    public virtual bool InUse { get; set; }
}