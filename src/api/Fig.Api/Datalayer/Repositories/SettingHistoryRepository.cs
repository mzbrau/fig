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
    
    public void Add(SettingValueBusinessEntity settingValue)
    {
        settingValue.SerializeAndEncrypt(_encryptionService);
        Save(settingValue);
    }

    public IList<SettingValueBusinessEntity> GetAll(Guid clientId, string settingName)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.ClientId), clientId));
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.SettingName), settingName));
        criteria.AddOrder(Order.Desc(nameof(SettingValueBusinessEntity.ChangedAt)));
        var result = criteria.List<SettingValueBusinessEntity>().ToList();
        result.ForEach(c => c.DeserializeAndDecrypt(_encryptionService));
        return result;
    }

    public IList<SettingValueBusinessEntity> GetValuesForEncryptionMigration(DateTime secretChangeDate)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(SettingValueBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(1000);
        criteria.SetLockMode(LockMode.Upgrade);
        var result = criteria.List<SettingValueBusinessEntity>().ToList();
        result.ForEach(c => c.DeserializeAndDecrypt(_encryptionService, true));
        return result;
    }

    public void UpdateValuesAfterEncryptionMigration(List<SettingValueBusinessEntity> values)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        using var transaction = Session.BeginTransaction();
        foreach (var value in values)
        {
            value.SerializeAndEncrypt(_encryptionService);
            Session.Update(value);
        }
            
        transaction.Commit();
        Session.Flush();
        
        foreach (var value in values)
            Session.Evict(value);
    }
}