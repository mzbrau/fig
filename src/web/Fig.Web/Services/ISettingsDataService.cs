using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models;

namespace Fig.Web.Services;

public interface ISettingsDataService
{
    Task LoadAllClients();

    IList<SettingsConfigurationModel> SettingsClients { get; }
}