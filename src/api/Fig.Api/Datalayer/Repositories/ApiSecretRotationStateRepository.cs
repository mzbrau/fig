using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ApiSecretRotationStateRepository : RepositoryBase<ApiSecretRotationStateBusinessEntity>, IApiSecretRotationStateRepository
{
    public ApiSecretRotationStateRepository(ISession session)
        : base(session)
    {
    }

    public async Task<ApiSecretRotationStateBusinessEntity?> GetForSecretPair(
        string currentSecretFingerprint,
        string previousSecretFingerprint,
        bool upgradeLock = false)
    {
        using var activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ApiSecretRotationStateBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ApiSecretRotationStateBusinessEntity.CurrentSecretFingerprint), currentSecretFingerprint));
        criteria.Add(Restrictions.Eq(nameof(ApiSecretRotationStateBusinessEntity.PreviousSecretFingerprint), previousSecretFingerprint));
        if (upgradeLock)
            criteria.SetLockMode(NHibernate.LockMode.Upgrade);

        return await criteria.UniqueResultAsync<ApiSecretRotationStateBusinessEntity>();
    }

    public async Task<ApiSecretRotationStateBusinessEntity?> GetLatestCompletedForCurrentSecret(string currentSecretFingerprint)
    {
        using var activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ApiSecretRotationStateBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ApiSecretRotationStateBusinessEntity.CurrentSecretFingerprint), currentSecretFingerprint));
        criteria.Add(Restrictions.Eq(
            nameof(ApiSecretRotationStateBusinessEntity.Status),
            ApiSecretRotationStatusPersistence.ToStorageValue(ApiSecretRotationMigrationStatus.MigrationCompleted)));
        criteria.AddOrder(Order.Desc(nameof(ApiSecretRotationStateBusinessEntity.CompletedAtUtc)));
        criteria.SetMaxResults(1);

        return await criteria.UniqueResultAsync<ApiSecretRotationStateBusinessEntity>();
    }

    public async Task SaveState(ApiSecretRotationStateBusinessEntity state)
    {
        await Save(state);
    }

    public async Task UpdateState(ApiSecretRotationStateBusinessEntity state)
    {
        await Update(state);
    }
}
