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
            AllowClientOverrides = model.AllowClientOverrides,
            ClientOverridesRegex = model.ClientOverridesRegex,
            DelayBeforeMemoryLeakMeasurementsMs = model.DelayBeforeMemoryLeakMeasurementsMs,
            IntervalBetweenMemoryLeakChecksMs = model.IntervalBetweenMemoryLeakChecksMs,
            MinimumDataPointsForMemoryLeakCheck = model.MinimumDataPointsForMemoryLeakCheck,
            WebApplicationBaseAddress = model.WebApplicationBaseAddress,
            UseAzureKeyVault = model.UseAzureKeyVault,
            AzureKeyVaultName = model.AzureKeyVaultName,
            PollIntervalOverride = model.PollIntervalOverride,
            AnalyzeMemoryUsage = model.AnalyzeMemoryUsage
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
            AllowClientOverrides = dataContract.AllowClientOverrides,
            ClientOverridesRegex = dataContract.ClientOverridesRegex,
            DelayBeforeMemoryLeakMeasurementsMs = dataContract.DelayBeforeMemoryLeakMeasurementsMs,
            IntervalBetweenMemoryLeakChecksMs = dataContract.IntervalBetweenMemoryLeakChecksMs,
            MinimumDataPointsForMemoryLeakCheck = dataContract.MinimumDataPointsForMemoryLeakCheck,
            WebApplicationBaseAddress = dataContract.WebApplicationBaseAddress,
            UseAzureKeyVault = dataContract.UseAzureKeyVault,
            AzureKeyVaultName = dataContract.AzureKeyVaultName,
            PollIntervalOverride = dataContract.PollIntervalOverride,
            AnalyzeMemoryUsage = dataContract.AnalyzeMemoryUsage
        };
    }
}