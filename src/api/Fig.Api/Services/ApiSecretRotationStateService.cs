using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Fig.Api.Datalayer.Repositories;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.ApiSecret;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class ApiSecretRotationStateService : IApiSecretRotationStateService
{
    private const string StageStatusPending = "Pending";
    private const string StageStatusInProgress = "InProgress";
    private const string StageStatusCompleted = "Completed";
    private const string StageStatusFailed = "Failed";
    private static readonly TimeSpan MinimumProgressUpdateInterval = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan LiveProgressExpiry = TimeSpan.FromMinutes(10);
    private const int MinimumProgressUpdateRecordInterval = 5;
    private static readonly ConcurrentDictionary<string, LiveMigrationProgress> LiveProgress = new();
    private readonly IOptionsMonitor<ApiSettings> _apiSettings;
    private readonly IApiSecretRotationStateRepository _repository;
    private readonly ILogger<ApiSecretRotationStateService> _logger;
    private readonly Dictionary<string, DateTime> _lastProgressUpdateUtc = new();
    private readonly Dictionary<string, int> _lastProgressUpdateRecords = new();

    public ApiSecretRotationStateService(
        IOptionsMonitor<ApiSettings> apiSettings,
        IApiSecretRotationStateRepository repository,
        ILogger<ApiSecretRotationStateService> logger)
    {
        _apiSettings = apiSettings;
        _repository = repository;
        _logger = logger;
    }

    public async Task<ApiSecretRotationSnapshot> GetSnapshot(bool upgradeLock = false)
    {
        var configuredSecrets = GetConfiguredSecrets();
        if (!configuredSecrets.IsRotationConfigured)
        {
            return new ApiSecretRotationSnapshot(
                ApiSecretRotationMigrationStatus.NotRequired,
                ApiSecretKeyOrder.CurrentOnly,
                false,
                false,
                false,
                null,
                null,
                null,
                null,
                null,
                0,
                null);
        }

        var state = await _repository.GetForSecretPair(
            configuredSecrets.CurrentFingerprint,
            configuredSecrets.PreviousFingerprint!,
            upgradeLock);

        var status = ParseStatus(state?.Status);
        var keyOrder = status == ApiSecretRotationMigrationStatus.MigrationCompleted
            ? ApiSecretKeyOrder.CurrentThenPrevious
            : ApiSecretKeyOrder.PreviousThenCurrent;
        var progress = DeserializeProgress(state?.ProgressJson);
        ApplyLiveProgress(configuredSecrets, status, progress);
        var currentStage = progress.Stages.FirstOrDefault(stage => stage.StageId == progress.CurrentStageId);

        return new ApiSecretRotationSnapshot(
            status,
            keyOrder,
            true,
            status is ApiSecretRotationMigrationStatus.PendingMigration or
                ApiSecretRotationMigrationStatus.MigrationInProgress or
                ApiSecretRotationMigrationStatus.MigrationFailed,
            status == ApiSecretRotationMigrationStatus.MigrationCompleted,
            state?.StartedAtUtc,
            state?.CompletedAtUtc,
            state?.FailedAtUtc,
            state?.StartedByHost,
            state?.LastCompletedStage,
            state?.ProcessedRecords ?? 0,
            state?.LastError,
            progress.CurrentStageId,
            currentStage?.DisplayName,
            currentStage?.StageIndex,
            progress.Stages.Any() ? progress.Stages.Count : null,
            currentStage?.ProcessedRecords ?? 0,
            currentStage?.TotalRecords,
            currentStage?.CurrentItem,
            progress.CurrentProgressMessage,
            progress.Stages.ToList());
    }

    public async Task<ApiSecretRotationStatusDataContract> GetStatus()
    {
        var snapshot = await GetSnapshot();
        return new ApiSecretRotationStatusDataContract
        {
            Status = snapshot.Status.ToString(),
            KeyOrder = snapshot.KeyOrder.ToString(),
            IsRotationConfigured = snapshot.IsRotationConfigured,
            IsMigrationRequired = snapshot.IsMigrationRequired,
            IsMigrationCompleted = snapshot.IsMigrationCompleted,
            StartedAtUtc = snapshot.StartedAtUtc,
            CompletedAtUtc = snapshot.CompletedAtUtc,
            FailedAtUtc = snapshot.FailedAtUtc,
            StartedByHost = snapshot.StartedByHost,
            LastCompletedStage = snapshot.LastCompletedStage,
            ProcessedRecords = snapshot.ProcessedRecords,
            LastError = snapshot.LastError,
            CurrentStageId = snapshot.CurrentStageId,
            CurrentStageName = snapshot.CurrentStageName,
            CurrentStageIndex = snapshot.CurrentStageIndex,
            TotalStages = snapshot.TotalStages,
            StageProcessedRecords = snapshot.StageProcessedRecords,
            StageTotalRecords = snapshot.StageTotalRecords,
            CurrentItem = snapshot.CurrentItem,
            CurrentProgressMessage = snapshot.CurrentProgressMessage,
            CompletionReminder = snapshot is { IsMigrationCompleted: true, IsRotationConfigured: true }
                ? "Migration is complete. Remove PreviousSecret after confirming all API hosts use the new secret. Users who logged in before the API secret change will be logged out when PreviousSecret is removed."
                : null,
            Stages = snapshot.Stages.ToList()
        };
    }

    public async Task<ApiSecretRotationSnapshot> MarkMigrationStarted()
    {
        var configuredSecrets = GetConfiguredSecrets();
        if (!configuredSecrets.IsRotationConfigured)
            throw new InvalidOperationException("API secret migration requires both Secret and PreviousSecret to be configured with different values.");

        var state = await _repository.GetForSecretPair(
            configuredSecrets.CurrentFingerprint,
            configuredSecrets.PreviousFingerprint!,
            true);

        if (state?.Status == ApiSecretRotationMigrationStatus.MigrationInProgress.ToString())
            throw new InvalidOperationException($"API secret migration is already in progress on host {state.StartedByHost}.");

        if (state?.Status == ApiSecretRotationMigrationStatus.MigrationCompleted.ToString())
            _logger.LogInformation("API secret migration has already completed for the configured secret pair. Running migration again to verify and remediate any stragglers.");

        var now = DateTime.UtcNow;
        _lastProgressUpdateUtc.Clear();
        _lastProgressUpdateRecords.Clear();
        if (state is null)
        {
            state = new ApiSecretRotationStateBusinessEntity
            {
                CurrentSecretFingerprint = configuredSecrets.CurrentFingerprint,
                PreviousSecretFingerprint = configuredSecrets.PreviousFingerprint!,
                CreatedAtUtc = now
            };
            ApplyMigrationStarted(state, now);
            await _repository.SaveState(state);
        }
        else
        {
            ApplyMigrationStarted(state, now);
            await _repository.UpdateState(state);
        }

        return await GetSnapshot();
    }

    public async Task InitializeMigrationProgress(IEnumerable<ApiSecretRotationStageProgressDataContract> stages)
    {
        var state = await GetRequiredCurrentState(true);
        var progress = new ApiSecretRotationProgressDataContract
        {
            Stages = stages
                .OrderBy(stage => stage.StageIndex)
                .Select(stage => new ApiSecretRotationStageProgressDataContract
                {
                    StageId = stage.StageId,
                    DisplayName = stage.DisplayName,
                    StageIndex = stage.StageIndex,
                    Status = StageStatusPending,
                    TotalRecords = stage.TotalRecords
                })
                .ToList()
        };

        state.ProgressJson = SerializeProgress(progress);
        state.LastCompletedStage = null;
        state.ProcessedRecords = 0;
        state.UpdatedAtUtc = DateTime.UtcNow;
        await _repository.UpdateState(state);
    }

    public async Task MarkMigrationStageStarted(string stageId,
        int? totalRecords = null,
        string? currentItem = null,
        string? currentAction = null)
    {
        var state = await GetRequiredCurrentState(true);
        ClearLiveProgress();
        var progress = DeserializeProgress(state.ProgressJson);
        var stage = GetStage(progress, stageId);
        var now = DateTime.UtcNow;

        stage.Status = StageStatusInProgress;
        stage.StartedAtUtc = now;
        stage.CompletedAtUtc = null;
        stage.FailedAtUtc = null;
        stage.Error = null;
        stage.CurrentItem = currentItem;
        stage.CurrentAction = currentAction;
        stage.TotalRecords = totalRecords ?? stage.TotalRecords;
        stage.ProcessedRecords = 0;
        progress.CurrentStageId = stage.StageId;
        progress.CurrentProgressMessage = BuildProgressMessage(stage);
        await PersistProgress(state, progress);
    }

    public async Task MarkMigrationProgress(string stageId,
        int processedRecords,
        int? totalRecords = null,
        string? currentItem = null,
        string? currentAction = null,
        bool force = false)
    {
        if (!ShouldPersistProgress(stageId, processedRecords, force))
            return;

        var state = await GetRequiredCurrentState(true);
        var progress = DeserializeProgress(state.ProgressJson);
        var stage = GetStage(progress, stageId);
        stage.Status = StageStatusInProgress;
        stage.ProcessedRecords = processedRecords;
        stage.TotalRecords = totalRecords ?? stage.TotalRecords;
        stage.CurrentItem = currentItem;
        stage.CurrentAction = currentAction;
        progress.CurrentStageId = stage.StageId;
        progress.CurrentProgressMessage = BuildProgressMessage(stage);
        await PersistProgress(state, progress);
    }

    public void ReportLiveMigrationProgress(string stageId,
        int processedRecords,
        int? totalRecords = null,
        string? currentItem = null,
        string? currentAction = null)
    {
        var configuredSecrets = GetConfiguredSecrets();
        if (!configuredSecrets.IsRotationConfigured)
            return;

        LiveProgress[GetLiveProgressKey(configuredSecrets)] = new LiveMigrationProgress(
            stageId,
            processedRecords,
            totalRecords,
            currentItem,
            currentAction,
            DateTime.UtcNow);
    }

    public async Task MarkMigrationStageCompleted(string stageId, int processedRecords, int? totalRecords = null)
    {
        var state = await GetRequiredCurrentState(true);
        ClearLiveProgress();
        var progress = DeserializeProgress(state.ProgressJson);
        var stage = GetStage(progress, stageId);
        var now = DateTime.UtcNow;

        stage.Status = StageStatusCompleted;
        stage.ProcessedRecords = processedRecords;
        stage.TotalRecords = totalRecords ?? stage.TotalRecords;
        stage.CurrentItem = null;
        stage.CurrentAction = null;
        stage.CompletedAtUtc = now;
        stage.FailedAtUtc = null;
        stage.Error = null;
        progress.CurrentStageId = stage.StageId;
        progress.CurrentProgressMessage = BuildProgressMessage(stage);
        state.LastCompletedStage = stage.DisplayName;
        await PersistProgress(state, progress);
    }

    public async Task MarkMigrationCompleted()
    {
        var state = await GetRequiredCurrentState(true);
        ClearLiveProgress();
        var progress = DeserializeProgress(state.ProgressJson);
        progress.CurrentStageId = null;
        progress.CurrentProgressMessage = "Migration complete. Remove PreviousSecret after all API hosts are aligned.";
        foreach (var stage in progress.Stages.Where(stage => stage.Status == StageStatusInProgress))
        {
            stage.Status = StageStatusCompleted;
            stage.CompletedAtUtc = DateTime.UtcNow;
            stage.CurrentItem = null;
            stage.CurrentAction = null;
        }

        state.ProgressJson = SerializeProgress(progress);
        state.ProcessedRecords = progress.Stages.Sum(stage => stage.ProcessedRecords);
        state.UpdatedAtUtc = DateTime.UtcNow;
        var now = DateTime.UtcNow;
        state.Status = ApiSecretRotationMigrationStatus.MigrationCompleted.ToString();
        state.CompletedAtUtc = now;
        state.FailedAtUtc = null;
        state.LastError = null;
        state.UpdatedAtUtc = now;
        await _repository.UpdateState(state);
    }

    public async Task MarkMigrationFailed(Exception exception)
    {
        var configuredSecrets = GetConfiguredSecrets();
        if (!configuredSecrets.IsRotationConfigured)
            return;

        var state = await _repository.GetForSecretPair(
            configuredSecrets.CurrentFingerprint,
            configuredSecrets.PreviousFingerprint!,
            true);

        if (state is null)
            return;

        var now = DateTime.UtcNow;
        ClearLiveProgress();
        state.Status = ApiSecretRotationMigrationStatus.MigrationFailed.ToString();
        state.FailedAtUtc = now;
        state.LastError = exception.Message;
        var progress = DeserializeProgress(state.ProgressJson);
        var currentStage = progress.Stages.FirstOrDefault(stage => stage.StageId == progress.CurrentStageId);
        if (currentStage is not null)
        {
            currentStage.Status = StageStatusFailed;
            currentStage.FailedAtUtc = now;
            currentStage.Error = exception.Message;
            currentStage.CurrentItem = null;
            currentStage.CurrentAction = null;
            progress.CurrentProgressMessage = $"Migration failed during {currentStage.DisplayName}: {exception.Message}";
            state.ProgressJson = SerializeProgress(progress);
        }

        state.ProcessedRecords = progress.Stages.Sum(stage => stage.ProcessedRecords);
        state.UpdatedAtUtc = now;
        await _repository.UpdateState(state);
    }

    private async Task<ApiSecretRotationStateBusinessEntity> GetRequiredCurrentState(bool upgradeLock)
    {
        var configuredSecrets = GetConfiguredSecrets();
        if (!configuredSecrets.IsRotationConfigured)
            throw new InvalidOperationException("API secret rotation is not configured.");

        var state = await _repository.GetForSecretPair(
            configuredSecrets.CurrentFingerprint,
            configuredSecrets.PreviousFingerprint!,
            upgradeLock);

        return state ?? throw new InvalidOperationException("API secret migration state was not found for the configured secret pair.");
    }

    private void ApplyMigrationStarted(ApiSecretRotationStateBusinessEntity state, DateTime now)
    {
        state.Status = ApiSecretRotationMigrationStatus.MigrationInProgress.ToString();
        ClearLiveProgress();
        state.StartedAtUtc = now;
        state.CompletedAtUtc = null;
        state.FailedAtUtc = null;
        state.LastError = null;
        state.StartedByHost = Environment.MachineName;
        state.LastCompletedStage = null;
        state.ProcessedRecords = 0;
        state.ProgressJson = null;
        state.UpdatedAtUtc = now;
    }

    private async Task PersistProgress(ApiSecretRotationStateBusinessEntity state,
        ApiSecretRotationProgressDataContract progress)
    {
        state.ProgressJson = SerializeProgress(progress);
        state.ProcessedRecords = progress.Stages.Sum(stage => stage.ProcessedRecords);
        state.LastCompletedStage = progress.Stages
            .Where(stage => stage.Status == StageStatusCompleted)
            .OrderBy(stage => stage.StageIndex)
            .LastOrDefault()
            ?.DisplayName;
        state.UpdatedAtUtc = DateTime.UtcNow;
        await _repository.UpdateState(state);
    }

    private bool ShouldPersistProgress(string stageId, int processedRecords, bool force)
    {
        var now = DateTime.UtcNow;
        if (force ||
            !_lastProgressUpdateUtc.TryGetValue(stageId, out var lastUpdatedUtc) ||
            !_lastProgressUpdateRecords.TryGetValue(stageId, out var lastRecords) ||
            processedRecords - lastRecords >= MinimumProgressUpdateRecordInterval ||
            now - lastUpdatedUtc >= MinimumProgressUpdateInterval)
        {
            _lastProgressUpdateUtc[stageId] = now;
            _lastProgressUpdateRecords[stageId] = processedRecords;
            return true;
        }

        return false;
    }

    private static void ApplyLiveProgress(ConfiguredSecrets configuredSecrets,
        ApiSecretRotationMigrationStatus status,
        ApiSecretRotationProgressDataContract progress)
    {
        var liveProgressKey = GetLiveProgressKey(configuredSecrets);
        if (status != ApiSecretRotationMigrationStatus.MigrationInProgress)
        {
            LiveProgress.TryRemove(liveProgressKey, out _);
            return;
        }

        if (!LiveProgress.TryGetValue(liveProgressKey, out var liveProgress))
            return;

        if (DateTime.UtcNow - liveProgress.UpdatedAtUtc > LiveProgressExpiry)
        {
            LiveProgress.TryRemove(liveProgressKey, out _);
            return;
        }

        var stage = progress.Stages.FirstOrDefault(a => a.StageId == liveProgress.StageId);
        if (stage is null || stage.Status == StageStatusCompleted || liveProgress.ProcessedRecords < stage.ProcessedRecords)
            return;

        stage.Status = StageStatusInProgress;
        stage.ProcessedRecords = liveProgress.ProcessedRecords;
        stage.TotalRecords = liveProgress.TotalRecords ?? stage.TotalRecords;
        stage.CurrentItem = liveProgress.CurrentItem ?? stage.CurrentItem;
        stage.CurrentAction = liveProgress.CurrentAction ?? stage.CurrentAction;
        progress.CurrentStageId = stage.StageId;
        progress.CurrentProgressMessage = BuildProgressMessage(stage);
    }

    private ApiSecretRotationProgressDataContract DeserializeProgress(string? progressJson)
    {
        if (string.IsNullOrWhiteSpace(progressJson))
            return new ApiSecretRotationProgressDataContract();

        try
        {
            return JsonConvert.DeserializeObject<ApiSecretRotationProgressDataContract>(progressJson, JsonSettings.FigDefault) ??
                   new ApiSecretRotationProgressDataContract();
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Failed to deserialize API secret rotation progress JSON. Progress will be reset for this status read.");
            return new ApiSecretRotationProgressDataContract();
        }
    }

    private static string SerializeProgress(ApiSecretRotationProgressDataContract progress)
    {
        return JsonConvert.SerializeObject(progress, JsonSettings.FigDefault);
    }

    private static ApiSecretRotationStageProgressDataContract GetStage(
        ApiSecretRotationProgressDataContract progress,
        string stageId)
    {
        return progress.Stages.FirstOrDefault(stage => stage.StageId == stageId)
               ?? throw new InvalidOperationException($"API secret migration progress stage '{stageId}' was not initialized.");
    }

    private static string BuildProgressMessage(ApiSecretRotationStageProgressDataContract stage)
    {
        var progressText = stage.TotalRecords.HasValue
            ? $"{stage.ProcessedRecords}/{stage.TotalRecords.Value} {stage.DisplayName} Complete"
            : $"{stage.ProcessedRecords} {stage.DisplayName} Complete";

        if (stage.Status == StageStatusCompleted)
            return progressText;

        if (!string.IsNullOrWhiteSpace(stage.CurrentItem))
        {
            var action = string.IsNullOrWhiteSpace(stage.CurrentAction)
                ? "Migrating"
                : stage.CurrentAction;
            return $"{progressText} - {action} {stage.CurrentItem}...";
        }

        if (!string.IsNullOrWhiteSpace(stage.CurrentAction))
            return stage.CurrentAction;

        return $"Migrating {stage.DisplayName}...";
    }

    private void ClearLiveProgress()
    {
        var configuredSecrets = GetConfiguredSecrets();
        if (configuredSecrets.IsRotationConfigured)
            LiveProgress.TryRemove(GetLiveProgressKey(configuredSecrets), out _);
    }

    private static string GetLiveProgressKey(ConfiguredSecrets configuredSecrets)
    {
        return $"{configuredSecrets.CurrentFingerprint}:{configuredSecrets.PreviousFingerprint}";
    }

    private ConfiguredSecrets GetConfiguredSecrets()
    {
        var currentSecret = _apiSettings.CurrentValue.GetDecryptedSecret();
        if (string.IsNullOrWhiteSpace(currentSecret))
            throw new InvalidOperationException("API secret is not configured.");

        var previousSecret = _apiSettings.CurrentValue.GetDecryptedPreviousSecret();
        var currentFingerprint = ComputeFingerprint(currentSecret);
        var previousFingerprint = string.IsNullOrWhiteSpace(previousSecret)
            ? null
            : ComputeFingerprint(previousSecret);

        var isRotationConfigured = !string.IsNullOrWhiteSpace(previousSecret) &&
                                   !string.Equals(currentSecret, previousSecret, StringComparison.Ordinal);

        return new ConfiguredSecrets(currentFingerprint, previousFingerprint, isRotationConfigured);
    }

    private static ApiSecretRotationMigrationStatus ParseStatus(string? value)
    {
        return Enum.TryParse<ApiSecretRotationMigrationStatus>(value, out var status)
            ? status
            : ApiSecretRotationMigrationStatus.PendingMigration;
    }

    private static string ComputeFingerprint(string secret)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(secret)));
    }

    private sealed record ConfiguredSecrets(
        string CurrentFingerprint,
        string? PreviousFingerprint,
        bool IsRotationConfigured);

    private sealed record LiveMigrationProgress(
        string StageId,
        int ProcessedRecords,
        int? TotalRecords,
        string? CurrentItem,
        string? CurrentAction,
        DateTime UpdatedAtUtc);
}
