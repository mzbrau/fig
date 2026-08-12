namespace Fig.Contracts.Dashboards
{
    public class DashboardLayoutCellDataContract
    {
        public string Id { get; set; } = string.Empty;

        public int X { get; set; }

        public int Y { get; set; }

        public int Width { get; set; } = 4;

        public int Height { get; set; } = 2;
    }
}
