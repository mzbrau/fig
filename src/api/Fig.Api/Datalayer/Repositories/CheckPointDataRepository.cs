using System.Diagnostics;
using Fig.Api.ExtensionMethods;
using Fig.Api.Observability;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class CheckPointDataRepository : RepositoryBase<CheckPointDataBusinessEntity>, ICheckPointDataRepository
{
    private readonly IEncryptionService _encryptionService;

    public CheckPointDataRepository(ISession session, IEncryptionService encryptionService) : base(session)
    {
        _encryptionService = encryptionService;
    }

    public async Task<CheckPointDataBusinessEntity?> GetData(Guid id)
    {
        var result = await Get(id, false);
        result?.Decrypt(_encryptionService);
        return result;
    }

    public async Task<Guid> Add(CheckPointDataBusinessEntity data)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        data.Encrypt(_encryptionService);
        return await Save(data);
    }

    public async Task<IEnumerable<CheckPointDataBusinessEntity>> GetCheckPointsForEncryptionMigration(DateTime secretChangeDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<CheckPointDataBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(CheckPointDataBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(50);
        criteria.SetLockMode(LockMode.Upgrade);

        var result = (await criteria.ListAsync<CheckPointDataBusinessEntity>()).ToList();
        result.ForEach(c => c.Decrypt(_encryptionService, true));
        return result;
    }

    public async Task UpdateCheckPointsAfterEncryptionMigration(List<CheckPointDataBusinessEntity> updatedCheckPoints)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var existingTransaction = Session.GetCurrentTransaction();
        var needsTransaction = existingTransaction == null || !existingTransaction.IsActive;
        var transaction = needsTransaction ? Session.BeginTransaction() : null;
        try
        {
            foreach (var checkPoint in updatedCheckPoints)
            {
                checkPoint.LastEncrypted = DateTime.UtcNow;
                checkPoint.Encrypt(_encryptionService);
                await Session.UpdateAsync(checkPoint);
            }

            if (transaction != null)
                await transaction.CommitAsync();
            await Session.FlushAsync();
            
            foreach (var checkPoint in updatedCheckPoints)
                await Session.EvictAsync(checkPoint);
        }
        finally
        {
            transaction?.Dispose();
        }
    }
    
    public async Task<int> DeleteOlderThan(DateTime cutoffDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        
        // First, get the DataIds from checkpoints that are older than the cutoff
        var dataIdsToDelete = await Session.CreateQuery(
                "select c.DataId from CheckPointBusinessEntity c where c.Timestamp < :cutoffDate")
            .SetParameter("cutoffDate", cutoffDate)
            .ListAsync<Guid>();
        
        if (!dataIdsToDelete.Any())
            return 0;
        
        // Delete checkpoint data that matches those IDs
        var deleteCount = await Session.CreateQuery(
                "delete from CheckPointDataBusinessEntity where Id in (:dataIds)")
            .SetParameterList("dataIds", dataIdsToDelete)
            .ExecuteUpdateAsync();
        
        await Session.FlushAsync();
        return deleteCount;
    }
}