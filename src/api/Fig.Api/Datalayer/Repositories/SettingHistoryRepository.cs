using System.Diagnostics;
using System.Security.Cryptography;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.Logging;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class SettingHistoryRepository : RepositoryBase<SettingValueBusinessEntity>, ISettingHistoryRepository
{
    private readonly IEncryptionService _encryptionService;
    private readonly ILogger<SettingHistoryRepository> _logger;

    public SettingHistoryRepository(ISession session, IEncryptionService encryptionService,
        ILogger<SettingHistoryRepository> logger)
        : base(session)
    {
        _encryptionService = encryptionService;
        _logger = logger;
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

    public async Task<int> RenameSetting(Guid clientId, string sourceSettingName, string targetSettingName)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var updatedRows = await Session.CreateQuery(
                "update SettingValueBusinessEntity set SettingName = :targetSettingName " +
                "where ClientId = :clientId and SettingName = :sourceSettingName")
            .SetParameter("targetSettingName", targetSettingName)
            .SetParameter("clientId", clientId)
            .SetParameter("sourceSettingName", sourceSettingName)
            .ExecuteUpdateAsync();

        await Session.FlushAsync();
        return updatedRows;
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

        var decrypted = new List<SettingValueBusinessEntity>(result.Count);
        foreach (var entry in result)
        {
            try
            {
                entry.DeserializeAndDecrypt(_encryptionService);
                decrypted.Add(entry);
            }
            catch (CryptographicException ex)
            {
                _logger.LogWarning(ex,
                    "Skipping history entry for setting {SettingName} (client {ClientId}) — unable to decrypt value",
                    entry.SettingName, entry.ClientId);
            }
        }

        return decrypted;
    }

    public async Task<IList<SettingValueBusinessEntity>> GetValuesForEncryptionMigration(DateTime secretChangeDate)
    {
        var result = await GetEncryptedValuesForEncryptionMigration(secretChangeDate);
        foreach (var value in result)
            value.DeserializeAndDecrypt(_encryptionService, true);
        return result;
    }

    public async Task<IList<SettingValueBusinessEntity>> GetEncryptedValuesForEncryptionMigration(DateTime secretChangeDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(SettingValueBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(1000);
        criteria.SetLockMode(LockMode.Upgrade);
        return (await criteria.ListAsync<SettingValueBusinessEntity>()).ToList();
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
