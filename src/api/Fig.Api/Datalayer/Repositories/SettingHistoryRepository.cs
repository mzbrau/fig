using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class SettingHistoryRepository : RepositoryBase<SettingValueBusinessEntity>, ISettingHistoryRepository
{
    public SettingHistoryRepository(IFigSessionFactory sessionFactory)
        : base(sessionFactory)
    {
    }


    public void Add(SettingValueBusinessEntity settingValue)
    {
        Save(settingValue);
    }

    public IEnumerable<SettingValueBusinessEntity> GetAll(Guid clientId, string settingName)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<SettingValueBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.ClientId), clientId));
        criteria.Add(Restrictions.Eq(nameof(SettingValueBusinessEntity.SettingName), settingName));
        criteria.AddOrder(Order.Desc(nameof(SettingValueBusinessEntity.ChangedAt)));
        return criteria.List<SettingValueBusinessEntity>();
    }
}