using Fig.Web.Models;
using Fig.Web.Models.Setting;
using Fig.Common.Events;
using Fig.Web.Events;

namespace Fig.Web.Services;

public class HeadingVisibilityManager : IHeadingVisibilityManager
{
    private readonly Dictionary<string, List<ISetting>> _headingSettings = new();
    private readonly Dictionary<string, HeadingType> _headingTypes = new();
    private readonly Dictionary<string, bool> _visibilityCache = new();

    public event EventHandler<string>? HeadingVisibilityChanged;

    public HeadingVisibilityManager(IEventDistributor eventDistributor)
    {
        // Subscribe to filter change events
        eventDistributor.Subscribe(EventConstants.SettingsFilterChanged, OnSettingsFilterChanged);
        eventDistributor.Subscribe(EventConstants.SettingsAdvancedVisibilityChanged, OnSettingsFilterChanged);
        eventDistributor.Subscribe(EventConstants.SettingsBaseValueFilterChanged, OnSettingsFilterChanged);
    }

    public void RegisterHeading(string headingId, List<ISetting> referencedSettings, HeadingType headingType)
    {
        _headingSettings[headingId] = referencedSettings;
        _headingTypes[headingId] = headingType;
        _visibilityCache.Remove(headingId); // Invalidate cache for this heading
    }

    public void UnregisterHeading(string headingId)
    {
        _headingSettings.Remove(headingId);
        _headingTypes.Remove(headingId);
        _visibilityCache.Remove(headingId);
    }

    public bool IsVisibleForHeading(string headingId)
    {
        // Check cache first
        if (_visibilityCache.TryGetValue(headingId, out var cachedResult))
        {
            return cachedResult;
        }

        // Calculate visibility
        var isVisible = CalculateVisibility(headingId);
        _visibilityCache[headingId] = isVisible;
        return isVisible;
    }

    public void InvalidateAll()
    {
        _visibilityCache.Clear();
        
        // Notify all headings that their visibility might have changed
        foreach (var headingId in _headingSettings.Keys.ToList())
        {
            HeadingVisibilityChanged?.Invoke(this, headingId);
        }
    }

    private bool CalculateVisibility(string headingId)
    {
        if (!_headingSettings.TryGetValue(headingId, out var referencedSettings))
        {
            return false;
        }

        if (!_headingTypes.TryGetValue(headingId, out var headingType))
        {
            return false;
        }

        // Custom action headings are always visible when they exist
        if (headingType == HeadingType.CustomAction)
        {
            return true;
        }

        // Setting headings are visible if any of their referenced settings are visible
        return referencedSettings.Any(setting => !setting.Hidden);
    }

    private void OnSettingsFilterChanged()
    {
        InvalidateAll();
    }
}
