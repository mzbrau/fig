using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface ISettingClientRepository
{
    Guid RegisterClient(SettingClientBusinessEntity client);

    void UpdateClient(SettingClientBusinessEntity client);

    IEnumerable<SettingClientBusinessEntity> GetAllClients();

    SettingClientBusinessEntity? GetClient(Guid id);

    SettingClientBusinessEntity? GetClient(string name, string? instance = null);

    IEnumerable<SettingClientBusinessEntity> GetAllInstancesOfClient(string name);

    void DeleteClient(SettingClientBusinessEntity client);
}