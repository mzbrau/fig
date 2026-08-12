namespace Fig.Contracts.Dashboards
{
    public class DashboardDataBindingDataContract
    {
        /// <summary>
        /// One of: "preset", "namedTransform", "inline".
        /// </summary>
        public string Mode { get; set; } = "inline";

        public string? PresetId { get; set; }

        public string? TransformId { get; set; }

        public string? InlineScript { get; set; }
    }
}
