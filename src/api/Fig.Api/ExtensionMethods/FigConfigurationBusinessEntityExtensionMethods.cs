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
        businessEntity.TimeMachineCleanupDays = dataContract.TimeMachineCleanupDays;
        businessEntity.EventLogsCleanupDays = dataContract.EventLogsCleanupDays;
        businessEntity.ApiStatusCleanupDays = dataContract.ApiStatusCleanupDays;
        businessEntity.SettingHistoryCleanupDays = dataContract.SettingHistoryCleanupDays;
        businessEntity.AllowMigrateFromMigrations = dataContract.AllowMigrateFromMigrations;
        businessEntity.EnableFigAssistant = dataContract.EnableFigAssistant;
        businessEntity.FigAssistantEndpoint = dataContract.FigAssistantEndpoint;
        businessEntity.FigAssistantModel = dataContract.FigAssistantModel;
        businessEntity.FigAssistantMaxToolIterations = dataContract.FigAssistantMaxToolIterations > 0
            ? dataContract.FigAssistantMaxToolIterations
            : 12;
        businessEntity.FigAssistantRequestTimeoutSeconds = dataContract.FigAssistantRequestTimeoutSeconds > 0
            ? dataContract.FigAssistantRequestTimeoutSeconds
            : 120;
        // FigAssistantAccessTokenEncrypted is handled separately in ConfigurationService
        // so placeholders do not wipe the stored secret.
    }
}