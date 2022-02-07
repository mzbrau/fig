using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models;

namespace Fig.Web.Services;

public interface ISettingsDataService
{
    Task LoadAllClients();

    IList<SettingClientConfigurationModel> SettingsClients { get; }

    Task DeleteClient(SettingClientConfigurationModel client);

    Task<int> SaveClient(SettingClientConfigurationModel client);
}