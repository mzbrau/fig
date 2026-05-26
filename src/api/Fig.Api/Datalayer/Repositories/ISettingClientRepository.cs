using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingClientRepository
{
    Task<Guid> RegisterClient(SettingClientBusinessEntity client);

    Task UpdateClient(SettingClientBusinessEntity client);

    Task<IList<SettingClientBusinessEntity>> GetAllClients(UserDataContract? requestingUser, bool upgradeLock = false, bool validateCode = true);

    Task<SettingClientReadResult> GetAllClientsBestEffort(UserDataContract? requestingUser, bool validateCode = true);

    Task<IList<SettingClientBusinessEntity>> GetAllClientsForEncryptionMigration(UserDataContract? requestingUser,
        Action<SettingClientMigrationLoadProgress>? progress = null);

    Task<SettingClientBusinessEntity?> GetClient(string name, string? instance = null);

    Task<SettingClientBusinessEntity?> GetClientForDeletion(string name, string? instance = null);

    Task<SettingClientBusinessEntity?> GetClientReadOnly(string name, string? instance = null);

    Task<IList<SettingClientBusinessEntity>> GetAllInstancesOfClient(string name, bool upgradeLock = true);

    Task<bool> HasAnyInstancesOfClient(string name);

    Task DeleteClient(SettingClientBusinessEntity client);

    Task<IList<(string Name, string Description)>> GetClientDescriptions(UserDataContract? requestingUser);
}

public sealed record SettingClientMigrationLoadProgress(
    int ProcessedClients,
    int TotalClients,
    string ClientName,
    string? Instance);