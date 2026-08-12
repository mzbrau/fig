using Fig.Contracts.Dashboards;

namespace Fig.Web.Dashboards.Facades;

public sealed class DashboardConcurrencyConflictException : Exception
{
    public DashboardConcurrencyConflictException(DashboardDataContract current)
        : base("The dashboard was modified by another user.")
    {
        Current = current;
    }

    public DashboardDataContract Current { get; }
}
