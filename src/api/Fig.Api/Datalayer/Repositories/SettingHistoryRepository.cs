using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingHistoryRepository : RepositoryBase<SettingValueBusinessEntity>, ISettingHistoryRepository
{
    private readonly IEncryptionService _encryptionService;

    public SettingHistoryRepository(ISession session, IEncryptionService encryptionService)
        : base(session)
    {
        _encryptionService = encryptionService;
    }
    
    public async Task Add(SettingValueBusinessEntity settingValue)
    {
        settingValue.SerializeAndEncrypt(_encryptionService);
        await Save(settingValue);
    }

    public async Task<IList<SettingValueBusinessEntity>> GetAll(Guid clientId, string settingName)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.ClientId), clientId));
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.SettingName), settingName));
        criteria.AddOrder(Order.Desc(nameof(SettingValueBusinessEntity.ChangedAt)));
        var result = (await criteria.ListAsync<SettingValueBusinessEntity>()).ToList();
        result.ForEach(c => c.DeserializeAndDecrypt(_encryptionService));
        return result;
    }

    public async Task<IList<SettingValueBusinessEntity>> GetLastChangedForAllClients()
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        // Fetch the most recent change entry per (ClientId, SettingName) with a deterministic tie-breaker.
        var hql = @"
            FROM SettingValueBusinessEntity sv
            WHERE sv.ChangedAt = (
                SELECT MAX(s2.ChangedAt)
                FROM SettingValueBusinessEntity s2
                WHERE s2.ClientId = sv.ClientId
                AND s2.SettingName = sv.SettingName
            )
            AND sv.Id = (
                SELECT MAX(s3.Id)
                FROM SettingValueBusinessEntity s3
                WHERE s3.ClientId = sv.ClientId
                AND s3.SettingName = sv.SettingName
                AND s3.ChangedAt = sv.ChangedAt
            )";

        var result = (await Session.CreateQuery(hql)
            .ListAsync<SettingValueBusinessEntity>()).ToList();

        result.ForEach(c => c.DeserializeAndDecrypt(_encryptionService));
        return result;
    }

    public async Task<IList<SettingValueBusinessEntity>> GetValuesForEncryptionMigration(DateTime secretChangeDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(SettingValueBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(1000);
        criteria.SetLockMode(LockMode.Upgrade);
        var result = (await criteria.ListAsync<SettingValueBusinessEntity>()).ToList();
        result.ForEach(c => c.DeserializeAndDecrypt(_encryptionService, true));
        return result;
    }

    public async Task UpdateValuesAfterEncryptionMigration(List<SettingValueBusinessEntity> values)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingTransaction = Session.GetCurrentTransaction();
        var needsTransaction = existingTransaction == null || !existingTransaction.IsActive;
        var transaction = needsTransaction ? Session.BeginTransaction() : null;
        try
        {
            foreach (var value in values)
            {
                value.SerializeAndEncrypt(_encryptionService);
                await Session.UpdateAsync(value);
            }
                
            if (transaction != null)
                await transaction.CommitAsync();
            await Session.FlushAsync();
            
            foreach (var value in values)
                await Session.EvictAsync(value);
        }
        finally
        {
            transaction?.Dispose();
        }
    }
    
    public async Task<int> DeleteOlderThan(DateTime cutoffDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var deleteCount = await Session.CreateQuery(
                "delete from SettingValueBusinessEntity where ChangedAt < :cutoffDate")
            .SetParameter("cutoffDate", cutoffDate)
            .ExecuteUpdateAsync();
        
        await Session.FlushAsync();
        return deleteCount;
    }
}