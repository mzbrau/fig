using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingClientRepository
{
    Task<Guid> RegisterClient(SettingClientBusinessEntity client);

    Task UpdateClient(SettingClientBusinessEntity client);

    Task<IList<SettingClientBusinessEntity>> GetAllClients(UserDataContract? requestingUser, bool upgradeLock);

    Task<SettingClientBusinessEntity?> GetClient(string name, string? instance = null);
    
    /// <summary>
    /// Gets a client for read-only operations without acquiring a database lock.
    /// Use this method when you only need to read client data and won't modify it.
    /// </summary>
    Task<SettingClientBusinessEntity?> GetClientReadOnly(string name, string? instance = null);

    Task<IList<SettingClientBusinessEntity>> GetAllInstancesOfClient(string name);
    
    /// <summary>
    /// Gets all instances of a client for read-only operations without acquiring database locks.
    /// Use this method when you only need to read client data and won't modify it.
    /// </summary>
    Task<IList<SettingClientBusinessEntity>> GetAllInstancesOfClientReadOnly(string name);

    Task DeleteClient(SettingClientBusinessEntity client);
    
    Task<IList<(string Name, string Description)>> GetClientDescriptions(UserDataContract? requestingUser);
}