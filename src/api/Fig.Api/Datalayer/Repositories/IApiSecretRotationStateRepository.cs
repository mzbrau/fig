using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IApiSecretRotationStateRepository
{
    Task<ApiSecretRotationStateBusinessEntity?> GetForSecretPair(
        string currentSecretFingerprint,
        string previousSecretFingerprint,
        bool upgradeLock = false);

    Task SaveState(ApiSecretRotationStateBusinessEntity state);

    Task UpdateState(ApiSecretRotationStateBusinessEntity state);
}
