using Fig.Web.Models.Setting;

namespace Fig.Web.Facades;

public interface ISettingClientFacade
{
    List<SettingClientConfigurationModel> SettingClients { get; }
    
    SettingClientConfigurationModel? SelectedSettingClient { get; set; }
    
    Task LoadAllClients();

    Task DeleteClient(SettingClientConfigurationModel client);

    Task<Dictionary<SettingClientConfigurationModel, List<string>>> SaveClient(
        SettingClientConfigurationModel client);

    Task<VerificationResultModel> RunVerification(SettingClientConfigurationModel clientName, string name);

    Task<List<SettingHistoryModel>> GetSettingHistory(SettingClientConfigurationModel client, string name);

    Task<List<VerificationResultModel>> GetVerificationHistory(SettingClientConfigurationModel client, string name);

    Task CheckClientRunSessions();
}