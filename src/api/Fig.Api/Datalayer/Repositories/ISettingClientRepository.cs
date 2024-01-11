using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingClientRepository
{
    Guid RegisterClient(SettingClientBusinessEntity client);

    void UpdateClient(SettingClientBusinessEntity client);

    IEnumerable<SettingClientBusinessEntity> GetAllClients(UserDataContract? requestingUser, bool upgradeLock);

    SettingClientBusinessEntity? GetClient(string name, string? instance = null);

    IEnumerable<SettingClientBusinessEntity> GetAllInstancesOfClient(string name);

    void DeleteClient(SettingClientBusinessEntity client);
}