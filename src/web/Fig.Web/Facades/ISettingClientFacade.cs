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
    /// Starts accumulating timing for a multi-client save batch. Call
    /// <see cref="CompleteSaveBatchAsync"/> after all <see cref="SaveClient"/> calls.
    /// </summary>
    void BeginSaveBatch(bool isSaveAll, int clientCount);

    /// <summary>
    /// Refreshes statuses/scheduling once (if not already done per-client) and reports save timing.
    /// </summary>
    Task CompleteSaveBatchAsync();

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