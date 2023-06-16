using Fig.Contracts.Configuration;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class FigConfigurationConverter : IFigConfigurationConverter
{
    public FigConfigurationDataContract Convert(FigConfigurationBusinessEntity configuration)
    {
        return new FigConfigurationDataContract
        {
            AllowNewRegistrations = configuration.AllowNewRegistrations,
            AllowUpdatedRegistrations = configuration.AllowUpdatedRegistrations,
            AllowFileImports = configuration.AllowFileImports,
            AllowOfflineSettings = configuration.AllowOfflineSettings,
            AllowDynamicVerifications = configuration.AllowDynamicVerifications,
            DelayBeforeMemoryLeakMeasurementsMs = configuration.DelayBeforeMemoryLeakMeasurementsMs,
            IntervalBetweenMemoryLeakChecksMs = configuration.IntervalBetweenMemoryLeakChecksMs,
            MinimumDataPointsForMemoryLeakCheck = configuration.MinimumDataPointsForMemoryLeakCheck,
            WebApplicationBaseAddress = configuration.WebApplicationBaseAddress
        };
    }
}