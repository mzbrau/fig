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
            AllowClientOverrides = configuration.AllowClientOverrides,
            ClientOverridesRegex = configuration.ClientOverridesRegex,
            WebApplicationBaseAddress = configuration.WebApplicationBaseAddress,
            UseAzureKeyVault = configuration.UseAzureKeyVault,
            AzureKeyVaultName = configuration.AzureKeyVaultName,
            PollIntervalOverride = configuration.PollIntervalOverride,
            AllowDisplayScripts = configuration.AllowDisplayScripts,
            EnableTimeMachine = configuration.EnableTimeMachine,
            TimelineDurationDays = configuration.TimelineDurationDays
        };
    }
}