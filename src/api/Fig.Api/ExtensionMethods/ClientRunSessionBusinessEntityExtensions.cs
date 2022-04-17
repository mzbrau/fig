using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Linq;

namespace Fig.Api.ExtensionMethods;

public static class ClientRunSessionBusinessEntityExtensions
{
    public static void Update(
        this ClientRunSessionBusinessEntity runSession, 
        StatusRequestDataContract statusRequest,
        string? hostname, 
        string? ipAddress)
    {
        runSession.Hostname = hostname;
        runSession.IpAddress = ipAddress;
        runSession.LastSeen = DateTime.UtcNow;
        runSession.LiveReload ??= statusRequest.LiveReload;
        runSession.PollIntervalMs ??= statusRequest.PollIntervalMs;
        runSession.UptimeSeconds = statusRequest.UptimeSeconds;
    }
    
    public static bool IsExpired(this ClientRunSessionBusinessEntity session)
    {
        double gracePeriodMs = 2 * session.PollIntervalMs.Value + 50;
        
        // This is a hack. Unsure why but after loading it back from SQL Lite db it comes back as a local time
        // rather than UTC time (that it is persisted as in the db. The work around is to convert it ot universal
        // before using it.
        // maybe related to: https://thomaslevesque.com/2015/06/28/how-to-retrieve-dates-as-utc-in-sqlite/
        var expiryTime = session.LastSeen.Value.ToUniversalTime() + TimeSpan.FromMilliseconds(gracePeriodMs);
        var result = expiryTime < DateTime.UtcNow;
        return result;
    }
}