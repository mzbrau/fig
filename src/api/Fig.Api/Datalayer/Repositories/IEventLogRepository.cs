using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IEventLogRepository
{
    Task Add(EventLogBusinessEntity log);

    Task<IList<EventLogBusinessEntity>> GetAllLogs(DateTime startDate,
        DateTime endDate,
        bool includeUserEvents,
        UserDataContract? requestingUser);

    Task<DateTime> GetEarliestEntry();

    Task<IList<EventLogBusinessEntity>> GetSettingChanges(DateTime startDate, DateTime endDate, string clientName,
        string? instance);

    Task<IList<EventLogBusinessEntity>> GetLogsForEncryptionMigration(DateTime secretChangeDate);

    Task UpdateLogsAfterEncryptionMigration(List<EventLogBusinessEntity> updatedLogs);
    
    Task<long> GetEventLogCount();
}