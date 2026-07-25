using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IClientRunSessionRepository
{
    Task<ClientRunSessionBusinessEntity?> GetRunSession(Guid id);
    
    Task UpdateRunSession(ClientRunSessionBusinessEntity runSession);

    /// <summary>
    /// Best-effort timestamp update without loading the session row (avoids HealthReportJson CLOBs).
    /// Only updates when the session belongs to <paramref name="clientId"/> and <paramref name="loadedUtc"/>
    /// is newer than the stored value (monotonic).
    /// </summary>
    Task TouchLastSettingLoadUtc(Guid clientId, Guid runSessionId, DateTime loadedUtc);
}