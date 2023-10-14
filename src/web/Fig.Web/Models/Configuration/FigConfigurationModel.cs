using Humanizer;

namespace Fig.Web.Models.Configuration;

public class FigConfigurationModel
{
    public bool AllowNewRegistrations { get; set; }

    public bool AllowUpdatedRegistrations { get; set; }

    public bool AllowFileImports { get; set; }

    public bool AllowOfflineSettings { get; set; }

    public bool AllowClientOverrides { get; set; }
        
    public string? ClientOverridesRegex { get; set; }
    
    public long DelayBeforeMemoryLeakMeasurementsMs { get; set; }

    public string DelayBeforeMemoryLeakMeasurementsHuman =>
        TimeSpan.FromMilliseconds(DelayBeforeMemoryLeakMeasurementsMs).Humanize(5);
        
    public long IntervalBetweenMemoryLeakChecksMs { get; set; }

    public string IntervalBetweenMemoryLeakChecksHuman =>
        TimeSpan.FromMilliseconds(IntervalBetweenMemoryLeakChecksMs).Humanize(5);
        
    public string? WebApplicationBaseAddress { get; set; }
    
    public int MinimumDataPointsForMemoryLeakCheck { get; set; }
    
    public bool UseAzureKeyVault { get; set; }
    
    public string? AzureKeyVaultName { get; set; }

    public FigConfigurationModel Clone()
    {
        return new FigConfigurationModel
        {
            AllowNewRegistrations = AllowNewRegistrations,
            AllowUpdatedRegistrations = AllowUpdatedRegistrations,
            AllowFileImports = AllowFileImports,
            AllowOfflineSettings = AllowOfflineSettings,
            AllowClientOverrides = AllowClientOverrides,
            ClientOverridesRegex = ClientOverridesRegex,
            DelayBeforeMemoryLeakMeasurementsMs = DelayBeforeMemoryLeakMeasurementsMs,
            IntervalBetweenMemoryLeakChecksMs = IntervalBetweenMemoryLeakChecksMs,
            MinimumDataPointsForMemoryLeakCheck = MinimumDataPointsForMemoryLeakCheck,
            WebApplicationBaseAddress = WebApplicationBaseAddress
        };
    }

    public void Revert(FigConfigurationModel model)
    {
        AllowNewRegistrations = model.AllowNewRegistrations;
        AllowUpdatedRegistrations = model.AllowUpdatedRegistrations;
        AllowFileImports = model.AllowFileImports;
        AllowOfflineSettings = model.AllowOfflineSettings;
        AllowClientOverrides = model.AllowClientOverrides;
        ClientOverridesRegex = model.ClientOverridesRegex;
        DelayBeforeMemoryLeakMeasurementsMs = model.DelayBeforeMemoryLeakMeasurementsMs;
        IntervalBetweenMemoryLeakChecksMs = model.IntervalBetweenMemoryLeakChecksMs;
        MinimumDataPointsForMemoryLeakCheck = model.MinimumDataPointsForMemoryLeakCheck;
        WebApplicationBaseAddress = model.WebApplicationBaseAddress;
    }
}