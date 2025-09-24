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
        businessEntity.WebApplicationBaseAddress = dataContract.WebApplicationBaseAddress;
        businessEntity.UseAzureKeyVault = dataContract.UseAzureKeyVault;
        businessEntity.AzureKeyVaultName = dataContract.AzureKeyVaultName;
        businessEntity.PollIntervalOverride = dataContract.PollIntervalOverride;
        businessEntity.AllowDisplayScripts = dataContract.AllowDisplayScripts;
        businessEntity.EnableTimeMachine = dataContract.EnableTimeMachine;
        businessEntity.TimelineDurationDays = dataContract.TimelineDurationDays;
    }
}