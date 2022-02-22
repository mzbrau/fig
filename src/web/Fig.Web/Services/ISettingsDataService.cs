using Fig.Web.Models.Setting;

namespace Fig.Web.Services;

public interface ISettingsDataService
{
    IList<SettingClientConfigurationModel> SettingsClients { get; }
    Task LoadAllClients();

    Task DeleteClient(SettingClientConfigurationModel client);

    Task<Dictionary<SettingClientConfigurationModel, List<string>>> SaveClient(
        SettingClientConfigurationModel client);

    Task<VerificationResultModel> RunVerification(SettingClientConfigurationModel clientName, string name);

    Task<List<SettingHistoryModel>> GetSettingHistory(SettingClientConfigurationModel client, string name);

    Task<List<VerificationResultModel>> GetVerificationHistory(SettingClientConfigurationModel client, string name);
}