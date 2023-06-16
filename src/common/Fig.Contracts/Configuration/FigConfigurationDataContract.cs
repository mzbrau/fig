namespace Fig.Contracts.Configuration
{
    public class FigConfigurationDataContract
    {
        public bool AllowNewRegistrations { get; set; }

        public bool AllowUpdatedRegistrations { get; set; }

        public bool AllowFileImports { get; set; }

        public bool AllowOfflineSettings { get; set; }

        public bool AllowDynamicVerifications { get; set; }
        
        public long DelayBeforeMemoryLeakMeasurementsMs { get; set; }
        
        public long IntervalBetweenMemoryLeakChecksMs { get; set; }
        
        public int MinimumDataPointsForMemoryLeakCheck { get; set; }
        
        public string? WebApplicationBaseAddress { get; set; }

        public override string ToString()
        {
            return $"AllowNewRegistrations:{AllowNewRegistrations}, " +
                   $"AllowUpdatedRegistrations:{AllowUpdatedRegistrations}, " +
                   $"AllowFileImports:{AllowFileImports}, " +
                   $"AllowOfflineSettings:{AllowOfflineSettings}, " +
                   $"AllowDynamicVerifications:{AllowDynamicVerifications}";
        }
    }
}