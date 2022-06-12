using System;

namespace Fig.Contracts.ImportExport
{
    public class CertificateMetadataDataContract
    {
        public virtual string Thumbprint { get; set; }
    
        public virtual DateTime ValidFrom { get; set; }
    
        public virtual DateTime ValidTo { get; set; }
    
        public virtual bool InUse { get; set; }
    }
}