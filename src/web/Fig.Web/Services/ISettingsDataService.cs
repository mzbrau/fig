using Fig.Contracts.SettingDefinitions;

namespace Fig.Web.Services;

public interface ISettingsDataService
{
    public IList<SettingsDefinitionDataContract> Services { get; }
}