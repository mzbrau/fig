using Fig.Contracts.Configuration;
using Fig.Web.Models.Configuration;

namespace Fig.Web.Converters;

public class FigConfigurationConverter : IFigConfigurationConverter
{
    public FigConfigurationDataContract Convert(FigConfigurationModel model)
    {
        return new FigConfigurationDataContract
        {
            AllowNewRegistrations = model.AllowNewRegistrations,
            AllowUpdatedRegistrations = model.AllowUpdatedRegistrations,
            AllowFileImports = model.AllowFileImports,
            AllowOfflineSettings = model.AllowOfflineSettings,
            AllowDynamicVerifications = model.AllowDynamicVerifications,
            AllowClientOverrides = model.AllowClientOverrides,
            ClientOverridesRegex = model.ClientOverridesRegex,
            DelayBeforeMemoryLeakMeasurementsMs = model.DelayBeforeMemoryLeakMeasurementsMs,
            IntervalBetweenMemoryLeakChecksMs = model.IntervalBetweenMemoryLeakChecksMs,
            MinimumDataPointsForMemoryLeakCheck = model.MinimumDataPointsForMemoryLeakCheck,
            WebApplicationBaseAddress = model.WebApplicationBaseAddress
        };
    }

    public FigConfigurationModel Convert(FigConfigurationDataContract dataContract)
    {
        return new FigConfigurationModel
        {
            AllowNewRegistrations = dataContract.AllowNewRegistrations,
            AllowUpdatedRegistrations = dataContract.AllowUpdatedRegistrations,
            AllowFileImports = dataContract.AllowFileImports,
            AllowOfflineSettings = dataContract.AllowOfflineSettings,
            AllowDynamicVerifications = dataContract.AllowDynamicVerifications,
            AllowClientOverrides = dataContract.AllowClientOverrides,
            ClientOverridesRegex = dataContract.ClientOverridesRegex,
            DelayBeforeMemoryLeakMeasurementsMs = dataContract.DelayBeforeMemoryLeakMeasurementsMs,
            IntervalBetweenMemoryLeakChecksMs = dataContract.IntervalBetweenMemoryLeakChecksMs,
            MinimumDataPointsForMemoryLeakCheck = dataContract.MinimumDataPointsForMemoryLeakCheck,
            WebApplicationBaseAddress = dataContract.WebApplicationBaseAddress
        };
    }
}