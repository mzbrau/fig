using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.Authentication;
using Fig.Contracts.ReleaseHighlights;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using ISession = NHibernate.ISession;
using IsolationLevel = System.Data.IsolationLevel;

namespace Fig.Api.Services;

public class ReleaseHighlightsService : AuthenticatedService, IReleaseHighlightsService
{
    private readonly IFigReleaseDiscoveryService _figReleaseDiscoveryService;
    private readonly IReleaseHighlightViewRepository _releaseHighlightViewRepository;
    private readonly ISession _session;

    public ReleaseHighlightsService(
        IFigReleaseDiscoveryService figReleaseDiscoveryService,
        IReleaseHighlightViewRepository releaseHighlightViewRepository,
        ISession session)
    {
        _figReleaseDiscoveryService = figReleaseDiscoveryService;
        _releaseHighlightViewRepository = releaseHighlightViewRepository;
        _session = session;
    }

    public async Task<ReleaseHighlightProgressDataContract> GetProgress()
    {
        var userId = GetAuthenticatedAdministratorId();
        var views = await _releaseHighlightViewRepository.GetViews(userId);
        var availableHighlights = new List<ReleaseHighlightCatalogItemDataContract>();
        var newestAvailableRelease = await _figReleaseDiscoveryService.GetNewestAvailableReleaseHighlight();
        if (newestAvailableRelease != null)
            availableHighlights.Add(newestAvailableRelease);

        return new ReleaseHighlightProgressDataContract(views.Select(Convert).ToList(), availableHighlights);
    }

    public async Task<ReleaseHighlightViewDataContract> RecordViewed(ReleaseHighlightViewedDataContract viewedHighlight)
    {
        if (viewedHighlight == null)
            throw new ArgumentNullException(nameof(viewedHighlight));

        var userId = GetAuthenticatedAdministratorId();
        var releaseVersion = viewedHighlight.ReleaseVersion?.Trim();
        var featureKey = viewedHighlight.FeatureKey?.Trim();

        if (string.IsNullOrWhiteSpace(releaseVersion))
            throw new ArgumentException("Release version is required.", nameof(viewedHighlight));

        if (string.IsNullOrWhiteSpace(featureKey))
            throw new ArgumentException("Feature key is required.", nameof(viewedHighlight));

        using var transaction = _session.BeginTransaction(IsolationLevel.Serializable);
        try
        {
            var existing = await _releaseHighlightViewRepository.GetView(userId, releaseVersion, featureKey, forUpdate: true);
            if (existing != null)
            {
                await transaction.CommitAsync();
                return Convert(existing);
            }

            var entity = new ReleaseHighlightViewBusinessEntity
            {
                UserId = userId,
                ReleaseVersion = releaseVersion,
                FeatureKey = featureKey,
                ViewedAtUtc = DateTime.UtcNow
            };

            await _releaseHighlightViewRepository.AddView(entity);
            await transaction.CommitAsync();
            return Convert(entity);
        }
        catch
        {
            if (transaction.IsActive)
                await transaction.RollbackAsync();
            throw;
        }
    }

    private Guid GetAuthenticatedAdministratorId()
    {
        if (AuthenticatedUser?.Role != Role.Administrator)
            throw new UnauthorizedAccessException("Only administrators can access release highlights.");

        return AuthenticatedUser.Id;
    }

    private static ReleaseHighlightViewDataContract Convert(ReleaseHighlightViewBusinessEntity entity)
    {
        return new ReleaseHighlightViewDataContract(entity.ReleaseVersion, entity.FeatureKey, entity.ViewedAtUtc);
    }
}
