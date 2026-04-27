using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IReleaseHighlightViewRepository
{
    Task<IList<ReleaseHighlightViewBusinessEntity>> GetViews(Guid userId);

    Task<ReleaseHighlightViewBusinessEntity?> GetView(Guid userId, string releaseVersion, string featureKey,
        bool forUpdate = false);

    Task<Guid> AddView(ReleaseHighlightViewBusinessEntity view);
}
