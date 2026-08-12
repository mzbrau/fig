using System;

namespace Fig.Contracts.Dashboards
{
    public class DashboardDataContract
    {
        public Guid? Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public bool AdminOnly { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime LastModifiedAt { get; set; }

        public string? LastModifiedBy { get; set; }

        public DashboardDefinitionDataContract Definition { get; set; } = new();
    }
}
