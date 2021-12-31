using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models;

namespace Fig.Web.Services;

public interface ISettingsDataService
{
    public IList<SettingsConfigurationModel> Services { get; }
}