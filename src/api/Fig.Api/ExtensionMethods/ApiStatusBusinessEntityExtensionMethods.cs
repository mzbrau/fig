using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class ApiStatusBusinessEntityExtensionMethods
{
    public static void Update(this ApiStatusBusinessEntity status, List<string> certificateThumbprints, DateTime startTime)
    {
        status.CertificatesInStore = certificateThumbprints;
        status.LastSeen = DateTime.UtcNow;
        status.UptimeSeconds = (startTime - DateTime.UtcNow).TotalSeconds;
    }
}