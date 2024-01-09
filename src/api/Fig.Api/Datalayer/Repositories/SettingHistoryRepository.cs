using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
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

    public IEnumerable<SettingValueBusinessEntity> GetAll(Guid clientId, string settingName)
    {
        var criteria = Session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.ClientId), clientId));
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.SettingName), settingName));
        criteria.AddOrder(Order.Desc(nameof(SettingValueBusinessEntity.ChangedAt)));
        var result = criteria.List<SettingValueBusinessEntity>().ToList();
        result.ForEach(c => c.DeserializeAndDecrypt(_encryptionService));
        return result;
    }

    public IEnumerable<SettingValueBusinessEntity> GetValuesForEncryptionMigration(DateTime secretChangeDate)
    {
        var criteria = Session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Le(nameof(SettingValueBusinessEntity.LastEncrypted), secretChangeDate));
        criteria.SetMaxResults(1000);
        var result = criteria.List<SettingValueBusinessEntity>().ToList();
        result.ForEach(c => c.DeserializeAndDecrypt(_encryptionService, true));
        return result;
    }

    public void UpdateValuesAfterEncryptionMigration(List<SettingValueBusinessEntity> values)
    {
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