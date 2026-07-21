using Fig.Contracts.SettingClients;
using Fig.Web.Models.Setting;

namespace Fig.Web.Facades;

public interface ISettingClientFacade
{
    List<SettingClientConfigurationModel> SettingClients { get; }
    
    List<ISearchableSetting> SearchableSettings { get; }
    
    SettingClientConfigurationModel? SelectedSettingClient { get; set; }

    string? PendingExpandedClientName { get; set; }
    
    event EventHandler<(string, double)> OnLoadProgressed;

    event EventHandler? OnDescriptionsLoaded;

    /// <param name="initializeScripts">
    /// When true (default), display scripts run before this method returns.
    /// When false, call <see cref="InitializeAllClientsAsync"/> after first paint so the UI can show processing status.
    /// </param>
    Task LoadAllClients(bool initializeScripts = true);

    /// <summary>
    /// Runs per-client InitializeAsync (validation + per-setting display scripts) and updates pending load-timing tallies.
    /// </summary>
    Task InitializeAllClientsAsync();

    Task DeleteClient(SettingClientConfigurationModel client);

    Task<Dictionary<SettingClientConfigurationModel, List<string>>> SaveClient(
        SettingClientConfigurationModel client,
        ChangeDetailsModel changeDetails,
        bool refreshAfterSave = true);

    /// <summary>
    /// Saves multiple clients with limited PUT parallelism, one post-save refresh, and one timing report.
    /// </summary>
    Task<SaveClientsBatchResult> SaveClientsBatch(
        IReadOnlyList<SettingClientConfigurationModel> clients,
        ChangeDetailsModel changeDetails,
        bool isSaveAll);

    Task<List<SettingHistoryModel>> GetSettingHistory(SettingClientConfigurationModel client, string name);

    Task CheckClientRunSessions();
    
    Task<ClientSecretChangeResponseDataContract> ChangeClientSecret(
        string clientName,
        string newClientSecret,
        DateTime oldClientSecretExpiry);

    Task LoadClientDescriptions();

    Task LoadAndNotifyAboutScheduledChanges();

    void ApplyPendingValueFromCompare(string clientName, string? instance, string settingName, string? rawValue);

    void MarkGroupsChanged();
}