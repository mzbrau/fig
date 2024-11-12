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

    public CheckPointDataBusinessEntity? GetData(Guid id)
    {
        var result = Get(id, false);
        result?.Decrypt(_encryptionService);
        return result;
    }

    public Guid Add(CheckPointDataBusinessEntity data)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        data.Encrypt(_encryptionService);
        return Save(data);
    }

    public IEnumerable<CheckPointDataBusinessEntity> GetCheckPointsForEncryptionMigration(DateTime secretChangeDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<CheckPointDataBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(CheckPointDataBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(50);
        criteria.SetLockMode(LockMode.Upgrade);

        var result = criteria.List<CheckPointDataBusinessEntity>().ToList();
        result.ForEach(c => c.Decrypt(_encryptionService, true));
        return result;
    }

    public void UpdateCheckPointsAfterEncryptionMigration(List<CheckPointDataBusinessEntity> updatedCheckPoints)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        foreach (var checkPoint in updatedCheckPoints)
        {
            checkPoint.LastEncrypted = DateTime.UtcNow;
            checkPoint.Encrypt(_encryptionService);
            Session.Update(checkPoint);
        }

        Session.Flush();
        
        foreach (var checkPoint in updatedCheckPoints)
            Session.Evict(checkPoint);
    }
}