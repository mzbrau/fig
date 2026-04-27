using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using NHibernate.Linq;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class ReleaseHighlightViewRepository : RepositoryBase<ReleaseHighlightViewBusinessEntity>, IReleaseHighlightViewRepository
{
    public ReleaseHighlightViewRepository(ISession session)
        : base(session)
    {
    }

    public async Task<IList<ReleaseHighlightViewBusinessEntity>> GetViews(Guid userId)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        return await Session.Query<ReleaseHighlightViewBusinessEntity>()
            .Where(x => x.UserId == userId)
            .OrderBy(x => x.ViewedAtUtc)
            .ToListAsync();
    }

    public async Task<ReleaseHighlightViewBusinessEntity?> GetView(Guid userId, string releaseVersion, string featureKey,
        bool forUpdate = false)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<ReleaseHighlightViewBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(ReleaseHighlightViewBusinessEntity.UserId), userId));
        criteria.Add(Restrictions.Eq(nameof(ReleaseHighlightViewBusinessEntity.ReleaseVersion), releaseVersion));
        criteria.Add(Restrictions.Eq(nameof(ReleaseHighlightViewBusinessEntity.FeatureKey), featureKey));
        if (forUpdate)
            criteria.SetLockMode(LockMode.Upgrade);

        return await criteria.UniqueResultAsync<ReleaseHighlightViewBusinessEntity>();
    }

    public async Task<Guid> AddView(ReleaseHighlightViewBusinessEntity view)
    {
        return await Save(view);
    }
}
