using Fig.Common;
using Fig.Contracts.Authentication;
using Fig.Contracts.ReleaseHighlights;
using Fig.Web.Services;

namespace Fig.Web.ReleaseHighlights;

public class ReleaseHighlightsCoordinator : IReleaseHighlightsCoordinator
{
    private readonly IAccountService _accountService;
    private readonly IReleaseHighlightsCatalog _catalog;
    private readonly IHttpService _httpService;
    private readonly IVersionHelper _versionHelper;
    private IReadOnlyList<ReleaseHighlightItem> _dynamicHighlights = Array.Empty<ReleaseHighlightItem>();
    private Guid? _loadedUserId;
    private HashSet<string> _viewedKeys = new(StringComparer.OrdinalIgnoreCase);

    public ReleaseHighlightsCoordinator(
        IAccountService accountService,
        IReleaseHighlightsCatalog catalog,
        IHttpService httpService,
        IVersionHelper versionHelper)
    {
        _accountService = accountService;
        _catalog = catalog;
        _httpService = httpService;
        _versionHelper = versionHelper;
    }

    public bool ShouldRetryAutoOpen { get; private set; }

    public async Task<ReleaseHighlightsDialogRequest?> GetAutoOpenDialog()
    {
        ShouldRetryAutoOpen = false;

        if (!TryGetAdministratorUserId(out _))
            return null;

        var progressLoaded = await EnsureProgressLoaded(forceRefresh: true);
        if (!progressLoaded)
        {
            ShouldRetryAutoOpen = true;
            return null;
        }

        var staticHighlights = GetAvailableStaticHighlights();
        var pendingVersions = staticHighlights
            .Select(item => item.ReleaseVersion)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(version => !IsVersionComplete(version, staticHighlights))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var pendingHighlights = staticHighlights
            .Where(item => pendingVersions.Contains(item.ReleaseVersion))
            .Concat(_dynamicHighlights.Where(item => !_viewedKeys.Contains(item.ViewKey)))
            .ToList();

        if (pendingHighlights.Count == 0)
            return null;

        var startIndex = pendingHighlights.FindIndex(item => !_viewedKeys.Contains(item.ViewKey));
        return new ReleaseHighlightsDialogRequest(pendingHighlights, startIndex < 0 ? 0 : startIndex, false);
    }

    public async Task<ReleaseHighlightsDialogRequest?> GetManualRecallDialog()
    {
        if (!TryGetAdministratorUserId(out _))
            return null;

        var progressLoaded = await EnsureProgressLoaded(forceRefresh: true);

        var availableHighlights = GetAvailableHighlights(progressLoaded);
        return new ReleaseHighlightsDialogRequest(availableHighlights, 0, true);
    }

    public async Task<bool> RecordViewed(ReleaseHighlightItem item)
    {
        if (!TryGetAdministratorUserId(out _))
            return false;

        if (!await EnsureProgressLoaded())
            return false;

        if (_viewedKeys.Contains(item.ViewKey))
            return true;

        var response = await _httpService.Post<ReleaseHighlightViewDataContract>(
            "releasehighlights/viewed",
            new ReleaseHighlightViewedDataContract(item.ReleaseVersion, item.FeatureKey));

        if (response == null)
            return false;

        _viewedKeys.Add(item.ViewKey);
        return true;
    }

    public void ResetSession()
    {
        _loadedUserId = null;
        _dynamicHighlights = Array.Empty<ReleaseHighlightItem>();
        _viewedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ShouldRetryAutoOpen = false;
    }

    private IReadOnlyList<ReleaseHighlightItem> GetAvailableHighlights(bool includeDynamicHighlights = true)
    {
        var staticHighlights = GetAvailableStaticHighlights();
        if (!includeDynamicHighlights || _dynamicHighlights.Count == 0)
            return staticHighlights;

        return staticHighlights
            .Concat(_dynamicHighlights)
            .ToList();
    }

    private IReadOnlyList<ReleaseHighlightItem> GetAvailableStaticHighlights()
    {
        var currentVersion = _versionHelper.GetVersion();
        return _catalog.GetAll()
            .Where(item => ReleaseHighlightsVersionComparer.IsReleasedOnOrBefore(item.ReleaseVersion, currentVersion))
            .OrderBy(item => ReleaseHighlightsVersionComparer.GetSortKey(item.ReleaseVersion))
            .ThenBy(item => item.SortOrder)
            .ToList();
    }

    private bool IsVersionComplete(string releaseVersion, IReadOnlyList<ReleaseHighlightItem> highlights)
    {
        var versionHighlights = highlights
            .Where(item => string.Equals(item.ReleaseVersion, releaseVersion, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return versionHighlights.Count > 0 && versionHighlights.All(item => _viewedKeys.Contains(item.ViewKey));
    }

    private async Task<bool> EnsureProgressLoaded(bool forceRefresh = false)
    {
        if (!TryGetAdministratorUserId(out var userId))
            return false;

        if (!forceRefresh && _loadedUserId == userId)
            return true;

        var progress = await _httpService.Get<ReleaseHighlightProgressDataContract>("releasehighlights", false);
        if (progress == null)
            return false;

        _loadedUserId = userId;
        _dynamicHighlights = progress.AvailableHighlights
            .Select(Convert)
            .OrderBy(item => ReleaseHighlightsVersionComparer.GetSortKey(item.ReleaseVersion))
            .ThenBy(item => item.SortOrder)
            .ToList();
        _viewedKeys = progress.ViewedHighlights
            .Select(view => $"{view.ReleaseVersion}:{view.FeatureKey}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return true;
    }

    private bool TryGetAdministratorUserId(out Guid userId)
    {
        var user = _accountService.AuthenticatedUser;
        if (user?.Role == Role.Administrator && user.Id.HasValue)
        {
            userId = user.Id.Value;
            return true;
        }

        userId = Guid.Empty;
        return false;
    }

    private static ReleaseHighlightItem Convert(ReleaseHighlightCatalogItemDataContract item)
    {
        return new ReleaseHighlightItem(
            item.ReleaseVersion,
            item.FeatureKey,
            item.Title,
            item.Description,
            item.ImagePath,
            item.SortOrder,
            item.ReadMoreUrl,
            item.MarkViewedOnDisplay);
    }
}
