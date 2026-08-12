namespace Fig.Web.Dashboards.Runtime;

public interface IDashboardDataProvider
{
    DashboardFigRoot Current { get; }

    DateTime? SettingsLastRefreshUtc { get; }

    DateTime? StatusLastRefreshUtc { get; }

    Task RefreshStatusAsync();

    Task RefreshSettingsAsync();

    /// <summary>
    /// Ensures both caches have been loaded at least once (does not force a refresh when already loaded).
    /// </summary>
    Task EnsureLoadedAsync();
}
