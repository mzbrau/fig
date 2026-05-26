using Fig.Contracts.ApiSecret;

namespace Fig.Api.Services;

internal static class EncryptionMigrationStages
{
    public const string SettingClientPreparation = "setting-client-preparation";
    public const string SettingClients = "setting-clients";
    public const string WebHookClients = "web-hook-clients";
    public const string EventLogs = "event-logs";
    public const string SettingHistory = "setting-history";
    public const string Checkpoints = "checkpoints";
    public const string DeferredChanges = "deferred-changes";

    public static IReadOnlyList<ApiSecretRotationStageProgressDataContract> CreateProgress()
    {
        return
        [
            Create(SettingClientPreparation, "Client Preparation", 1),
            Create(SettingClients, "Clients", 2),
            Create(WebHookClients, "Web Hook Clients", 3),
            Create(EventLogs, "Event Logs", 4),
            Create(SettingHistory, "Setting History Records", 5),
            Create(Checkpoints, "Checkpoints", 6),
            Create(DeferredChanges, "Deferred Changes", 7)
        ];
    }

    private static ApiSecretRotationStageProgressDataContract Create(string stageId, string displayName, int stageIndex)
    {
        return new ApiSecretRotationStageProgressDataContract
        {
            StageId = stageId,
            DisplayName = displayName,
            StageIndex = stageIndex
        };
    }
}
