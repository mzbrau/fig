namespace Fig.Web.Attributes;

/// <summary>
/// Marks a page as accessible to the Dashboard role (dashboards-only chrome).
/// Used by <see cref="Routing.AppRouteView"/> so Dashboard users are not redirected away.
/// </summary>
public class DashboardOnlyAttribute : Attribute
{
}
