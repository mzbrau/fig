using System.Security.Cryptography;
using System.Text;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.ApiSecret;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Options;

namespace Fig.Api.Services;

public class ApiSecretRotationStateService : IApiSecretRotationStateService
{
    private readonly IOptionsMonitor<ApiSettings> _apiSettings;
    private readonly IApiSecretRotationStateRepository _repository;
    private readonly ILogger<ApiSecretRotationStateService> _logger;

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
            state?.LastError);
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
            LastError = snapshot.LastError
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

    public async Task MarkMigrationStageCompleted(string stage, int processedRecords)
    {
        var state = await GetRequiredCurrentState(true);
        state.LastCompletedStage = stage;
        state.ProcessedRecords += processedRecords;
        state.UpdatedAtUtc = DateTime.UtcNow;
        await _repository.UpdateState(state);
    }

    public async Task MarkMigrationCompleted()
    {
        var state = await GetRequiredCurrentState(true);
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
        state.Status = ApiSecretRotationMigrationStatus.MigrationFailed.ToString();
        state.FailedAtUtc = now;
        state.LastError = exception.Message;
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
        state.StartedAtUtc = now;
        state.CompletedAtUtc = null;
        state.FailedAtUtc = null;
        state.LastError = null;
        state.StartedByHost = Environment.MachineName;
        state.LastCompletedStage = null;
        state.ProcessedRecords = 0;
        state.UpdatedAtUtc = now;
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
}
