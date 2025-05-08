using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingClientRepository
{
    Task<Guid> RegisterClient(SettingClientBusinessEntity client);

    Task UpdateClient(SettingClientBusinessEntity client);

    Task<IList<SettingClientBusinessEntity>> GetAllClients(UserDataContract? requestingUser, bool upgradeLock);

    Task<SettingClientBusinessEntity?> GetClient(string name, string? instance = null);

    Task<IList<SettingClientBusinessEntity>> GetAllInstancesOfClient(string name);

    Task DeleteClient(SettingClientBusinessEntity client);

    Task<List<(string Name, string Description)>> GetAllClientDescriptions();
}