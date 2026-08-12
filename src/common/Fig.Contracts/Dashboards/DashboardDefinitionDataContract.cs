using System.Collections.Generic;

namespace Fig.Contracts.Dashboards
{
    public class DashboardDefinitionDataContract
    {
        public int SchemaVersion { get; set; } = 1;

        public DashboardRefreshDataContract Refresh { get; set; } = new();

        public List<DashboardTransformDataContract> Transforms { get; set; } = new();

        public List<DashboardComponentInstanceDataContract> Components { get; set; } = new();

        public List<DashboardLayoutCellDataContract> Layout { get; set; } = new();
    }
}
