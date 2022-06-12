namespace Fig.Api.Encryption;

public class CertificateStatus
{
    public CertificateStatus(string inUseThumbprint, string newestThumbprint)
    {
        InUseThumbprint = inUseThumbprint;
        NewestThumbprint = newestThumbprint;
    }
    
    public string InUseThumbprint { get; }

    public string NewestThumbprint { get; }

    public bool RequiresMigration => InUseThumbprint != NewestThumbprint;
}