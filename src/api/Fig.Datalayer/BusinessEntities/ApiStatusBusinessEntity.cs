using Newtonsoft.Json;

namespace Fig.Datalayer.BusinessEntities;

public class ApiStatusBusinessEntity
{
    private string? _certificatesInStoreJson;

    public virtual Guid? Id { get; set; }

    public virtual Guid RuntimeId { get; set; }

    public virtual double UptimeSeconds { get; set; }

    public virtual DateTime LastSeen { get; set; }

    public virtual string? IpAddress { get; set; }

    public virtual string? Hostname { get; set; }

    public virtual string Version { get; set; }

    public virtual IList<string> CertificatesInStore { get; set; }

    public virtual bool IsActive { get; set; }

    public virtual string? CertificatesInStoreJson
    {
        get
        {
            if (CertificatesInStore == null)
                return null;

            _certificatesInStoreJson = JsonConvert.SerializeObject(CertificatesInStore);
            return _certificatesInStoreJson;
        }
        set
        {
            if (_certificatesInStoreJson != value)
                CertificatesInStore = value != null
                    ? JsonConvert.DeserializeObject<IList<string>>(value)
                    : Array.Empty<string>();
        }
    }
}