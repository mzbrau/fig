using Fig.Contracts.SettingDefinitions;
using Fig.Web.Models.Setting;

namespace Fig.Web.Converters;

public interface ISettingsDefinitionConverter
{
    void ResetModelBuildTiming();

    long TakeModelBuildElapsedMs();

    Task<List<SettingClientConfigurationModel>> Convert(IList<SettingsClientDefinitionDataContract> settingDataContracts, Action<(string, double)> updateProgress);
}