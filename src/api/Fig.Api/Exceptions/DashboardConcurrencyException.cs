using Fig.Contracts.Dashboards;

namespace Fig.Api.Exceptions;

public class DashboardConcurrencyException : Exception
{
    public DashboardConcurrencyException(DashboardDataContract current)
        : base("Dashboard was modified by another user. Reload and try again.")
    {
        Current = current;
    }

    public DashboardDataContract Current { get; }
}
