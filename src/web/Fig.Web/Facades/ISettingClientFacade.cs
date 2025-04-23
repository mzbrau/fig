using Fig.Contracts.SettingClients;
using Fig.Web.Models.Setting;

namespace Fig.Web.Facades;

public interface ISettingClientFacade
{
    List<SettingClientConfigurationModel> SettingClients { get; }
    
    SettingClientConfigurationModel? SelectedSettingClient { get; set; }
    event EventHandler<double> OnLoadProgressed;

    Task LoadAllClients();

    Task DeleteClient(SettingClientConfigurationModel client);

    Task<Dictionary<SettingClientConfigurationModel, List<string>>> SaveClient(
        SettingClientConfigurationModel client, ChangeDetailsModel changeDetails);

    Task<VerificationResultModel> RunVerification(SettingClientConfigurationModel clientName, string name);

    Task<List<SettingHistoryModel>> GetSettingHistory(SettingClientConfigurationModel client, string name);

    Task<List<VerificationResultModel>> GetVerificationHistory(SettingClientConfigurationModel client, string name);

    Task CheckClientRunSessions();
    
    Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(
        string clientName,
        string newClientSecret,
        DateTime oldClientSecretExpiry);
}