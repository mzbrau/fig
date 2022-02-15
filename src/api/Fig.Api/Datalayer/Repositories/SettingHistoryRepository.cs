using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class SettingHistoryRepository : RepositoryBase<SettingValueBusinessEntity>, ISettingHistoryRepository
{
    private readonly IEncryptionService _encryptionService;

    public SettingHistoryRepository(IFigSessionFactory sessionFactory, IEncryptionService encryptionService)
        : base(sessionFactory)
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
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.ClientId), clientId));
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.SettingName), settingName));
        criteria.AddOrder(Order.Desc(nameof(SettingValueBusinessEntity.ChangedAt)));
        var result = criteria.List<SettingValueBusinessEntity>().ToList();
        result.ForEach(c => c.DeserializeAndDecrypt(_encryptionService));
        return result;
    }
}