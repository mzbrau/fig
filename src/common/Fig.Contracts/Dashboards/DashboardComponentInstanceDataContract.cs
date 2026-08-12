using Newtonsoft.Json.Linq;

namespace Fig.Contracts.Dashboards
{
    public class DashboardComponentInstanceDataContract
    {
        public string Id { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;

        public JObject Config { get; set; } = new();

        public DashboardDataBindingDataContract DataBinding { get; set; } = new();
    }
}
