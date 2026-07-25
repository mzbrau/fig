using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IClientRunSessionRepository
{
    Task<ClientRunSessionBusinessEntity?> GetRunSession(Guid id);
    
    Task UpdateRunSession(ClientRunSessionBusinessEntity runSession);

    /// <summary>
    /// Best-effort timestamp update without loading the session row (avoids HealthReportJson CLOBs).
    /// </summary>
    Task TouchLastSettingLoadUtc(Guid runSessionId, DateTime loadedUtc);
}