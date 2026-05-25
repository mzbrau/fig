using Fig.Contracts.ApiSecret;

namespace Fig.Api.Services;

internal static class EncryptionMigrationStages
{
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
            Create(SettingClients, "Clients", 1),
            Create(WebHookClients, "Web Hook Clients", 2),
            Create(EventLogs, "Event Logs", 3),
            Create(SettingHistory, "Setting History Records", 4),
            Create(Checkpoints, "Checkpoints", 5),
            Create(DeferredChanges, "Deferred Changes", 6)
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
