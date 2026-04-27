using Fig.Common;
using Fig.Contracts.Authentication;
using Fig.Contracts.ReleaseHighlights;
using Fig.Web.Services;

namespace Fig.Web.ReleaseHighlights;

public class ReleaseHighlightsCoordinator : IReleaseHighlightsCoordinator
{
    private const string CacheKey = "release-highlights-cache";
    private readonly IAccountService _accountService;
    private readonly IReleaseHighlightsCatalog _catalog;
    private readonly IHttpService _httpService;
    private readonly ILocalStorageService _localStorageService;
    private readonly IVersionHelper _versionHelper;
    private Guid? _loadedUserId;
    private HashSet<string> _viewedKeys = new(StringComparer.OrdinalIgnoreCase);

    public ReleaseHighlightsCoordinator(
        IAccountService accountService,
        IReleaseHighlightsCatalog catalog,
        IHttpService httpService,
        ILocalStorageService localStorageService,
        IVersionHelper versionHelper)
    {
        _accountService = accountService;
        _catalog = catalog;
        _httpService = httpService;
        _localStorageService = localStorageService;
        _versionHelper = versionHelper;
    }

    public bool ShouldRetryAutoOpen { get; private set; }

    public async Task<ReleaseHighlightsDialogRequest?> GetAutoOpenDialog()
    {
        ShouldRetryAutoOpen = false;

        if (!TryGetAdministratorUserId(out var userId))
            return null;

        var availableHighlights = GetAvailableHighlights();
        if (availableHighlights.Count == 0)
            return null;

        var relevantVersions = availableHighlights
            .Select(x => x.ReleaseVersion)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var cache = await _localStorageService.GetItem<ReleaseHighlightsCacheState>(CacheKey);
        if (cache?.UserId == userId && relevantVersions.All(version => cache.CompletedVersions.Contains(version, StringComparer.OrdinalIgnoreCase)))
            return null;

        var progressLoaded = await EnsureProgressLoaded(forceRefresh: true);
        if (!progressLoaded)
        {
            ShouldRetryAutoOpen = true;
            return null;
        }

        var pendingVersions = relevantVersions
            .Where(version => !IsVersionComplete(version, availableHighlights))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        await PersistCache(userId, availableHighlights);

        if (pendingVersions.Count == 0)
            return null;

        var pendingHighlights = availableHighlights
            .Where(item => pendingVersions.Contains(item.ReleaseVersion))
            .ToList();

        var startIndex = pendingHighlights.FindIndex(item => !_viewedKeys.Contains(item.ViewKey));
        return new ReleaseHighlightsDialogRequest(pendingHighlights, startIndex < 0 ? 0 : startIndex, false);
    }

    public async Task<ReleaseHighlightsDialogRequest?> GetManualRecallDialog()
    {
        if (!TryGetAdministratorUserId(out _))
            return null;

        await EnsureProgressLoaded(forceRefresh: true);

        var availableHighlights = GetAvailableHighlights();
        return new ReleaseHighlightsDialogRequest(availableHighlights, 0, true);
    }

    public async Task<bool> RecordViewed(ReleaseHighlightItem item)
    {
        if (!TryGetAdministratorUserId(out var userId))
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
        await PersistCache(userId, GetAvailableHighlights());
        return true;
    }

    public void ResetSession()
    {
        _loadedUserId = null;
        _viewedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        ShouldRetryAutoOpen = false;
    }

    private IReadOnlyList<ReleaseHighlightItem> GetAvailableHighlights()
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

    private async Task PersistCache(Guid userId, IReadOnlyList<ReleaseHighlightItem> highlights)
    {
        var completedVersions = highlights
            .Select(item => item.ReleaseVersion)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(version => IsVersionComplete(version, highlights))
            .ToList();

        await _localStorageService.SetItem(CacheKey, new ReleaseHighlightsCacheState
        {
            UserId = userId,
            CompletedVersions = completedVersions
        });
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
}
