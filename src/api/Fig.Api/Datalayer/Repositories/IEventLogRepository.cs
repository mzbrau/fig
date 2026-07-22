using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IEventLogRepository
{
    Task Add(EventLogBusinessEntity log);

    /// <summary>
    /// Persists an event log in its own session/transaction so it survives ambient request rollback
    /// (e.g. failed login / invalid client secret that throw after logging).
    /// </summary>
    Task AddCommitted(EventLogBusinessEntity log);

    Task<IList<EventLogBusinessEntity>> GetAllLogs(DateTime startDate,
        DateTime endDate,
        bool includeUserEvents,
        UserDataContract requestingUser);

    Task<DateTime> GetEarliestEntry();

    Task<IList<EventLogBusinessEntity>> GetSettingChanges(DateTime startDate, DateTime endDate, string clientName,
        string? instance);

    Task<IList<EventLogBusinessEntity>> GetClientSettingChanges(DateTime startDate, DateTime endDate, string clientName,
        string? instance, UserDataContract requestingUser);

    Task<IList<EventLogBusinessEntity>> GetClientEvents(
        DateTime startDate,
        DateTime endDate,
        string clientName,
        string? instance,
        IReadOnlyCollection<string>? eventTypes = null);

    Task<IList<EventLogBusinessEntity>> GetLogsForAuthenticatedUser(
        DateTime startDate,
        DateTime endDate,
        string username);

    /// <summary>
    /// Query of events by type with optional client/instance filter.
    /// Events with an empty client name are always included; otherwise the caller's ClientFilter applies.
    /// </summary>
    Task<IList<EventLogBusinessEntity>> GetEventsByTypes(
        DateTime startDate,
        DateTime endDate,
        IReadOnlyCollection<string> eventTypes,
        UserDataContract requestingUser,
        string? clientName = null,
        string? instance = null);

    Task<IList<EventLogBusinessEntity>> GetLogsForEncryptionMigration(DateTime secretChangeDate);

    Task<IList<EventLogBusinessEntity>> GetEncryptedLogsForEncryptionMigration(DateTime secretChangeDate);

    Task UpdateLogsAfterEncryptionMigration(List<EventLogBusinessEntity> updatedLogs);
    
    Task<long> GetEventLogCount();
    
    Task<int> DeleteOlderThan(DateTime cutoffDate);
}