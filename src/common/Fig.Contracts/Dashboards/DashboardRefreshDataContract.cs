namespace Fig.Contracts.Dashboards
{
    public class DashboardRefreshDataContract
    {
        public int SettingsSeconds { get; set; } = 600;

        public int StatusSeconds { get; set; } = 60;
    }
}
