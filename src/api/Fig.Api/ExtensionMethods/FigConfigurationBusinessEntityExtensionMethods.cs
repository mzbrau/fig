using Fig.Contracts.Configuration;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class FigConfigurationBusinessEntityExtensionMethods
{
    public static void Update(this FigConfigurationBusinessEntity businessEntity, FigConfigurationDataContract dataContract)
    {
        businessEntity.AllowNewRegistrations = dataContract.AllowNewRegistrations;
        businessEntity.AllowUpdatedRegistrations = dataContract.AllowUpdatedRegistrations;
        businessEntity.AllowFileImports = dataContract.AllowFileImports;
        businessEntity.AllowOfflineSettings = dataContract.AllowOfflineSettings;
        businessEntity.AllowClientOverrides = dataContract.AllowClientOverrides;
        businessEntity.ClientOverridesRegex = dataContract.ClientOverridesRegex;
        businessEntity.DelayBeforeMemoryLeakMeasurementsMs = dataContract.DelayBeforeMemoryLeakMeasurementsMs;
        businessEntity.IntervalBetweenMemoryLeakChecksMs = dataContract.IntervalBetweenMemoryLeakChecksMs;
        businessEntity.MinimumDataPointsForMemoryLeakCheck = dataContract.MinimumDataPointsForMemoryLeakCheck;
        businessEntity.WebApplicationBaseAddress = dataContract.WebApplicationBaseAddress;
        businessEntity.UseAzureKeyVault = dataContract.UseAzureKeyVault;
        businessEntity.AzureKeyVaultName = dataContract.AzureKeyVaultName;
    }
}