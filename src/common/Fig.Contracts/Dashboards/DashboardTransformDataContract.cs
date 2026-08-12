using System.Collections.Generic;

namespace Fig.Contracts.Dashboards
{
    public class DashboardTransformDataContract
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Script { get; set; } = string.Empty;

        public List<string> DependsOn { get; set; } = new();
    }
}
