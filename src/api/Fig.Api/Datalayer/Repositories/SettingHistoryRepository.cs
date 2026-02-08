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

    public async Task<IList<SettingValueBusinessEntity>> GetLastChangedForAllSettings(Guid clientId)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();

        // Use a JOIN with an inline subquery to find the maximum ChangedAt for each SettingName.
        // This is more efficient than a correlated subquery as it calculates the max values once.
        var hql = @"
            FROM SettingValueBusinessEntity sv
            WHERE sv.Id IN (
                SELECT sv2.Id
                FROM SettingValueBusinessEntity sv2
                INNER JOIN (
                    SELECT sub.SettingName, MAX(sub.ChangedAt) as MaxChangedAt
                    FROM SettingValueBusinessEntity sub
                    WHERE sub.ClientId = :clientId
                    GROUP BY sub.SettingName
                ) maxDates
                ON sv2.SettingName = maxDates.SettingName 
                AND sv2.ChangedAt = maxDates.MaxChangedAt
                WHERE sv2.ClientId = :clientId
            )";

        var result = (await Session.CreateQuery(hql)
            .SetParameter("clientId", clientId)
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