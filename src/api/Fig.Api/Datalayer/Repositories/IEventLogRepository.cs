using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IEventLogRepository
{
    void Add(EventLogBusinessEntity log);

    IEnumerable<EventLogBusinessEntity> GetAllLogs(DateTime startDate,
        DateTime endDate,
        bool includeUserEvents,
        UserDataContract? requestingUser);

    DateTime GetEarliestEntry();

    IEnumerable<EventLogBusinessEntity> GetSettingChanges(DateTime startDate, DateTime endDate, string clientName,
        string? instance);

    IEnumerable<EventLogBusinessEntity> GetLogsForEncryptionMigration(DateTime secretChangeDate);

    void UpdateLogsAfterEncryptionMigration(List<EventLogBusinessEntity> updatedLogs);
    
    long GetEventLogCount();
}