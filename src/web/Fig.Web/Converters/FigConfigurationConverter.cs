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
            WebApplicationBaseAddress = model.WebApplicationBaseAddress,
            UseAzureKeyVault = model.UseAzureKeyVault,
            AzureKeyVaultName = model.AzureKeyVaultName,
            PollIntervalOverride = model.PollIntervalOverride,
            AllowDisplayScripts = model.AllowDisplayScripts,
            EnableTimeMachine = model.EnableTimeMachine,
            TimelineDurationDays = model.TimelineDurationDays
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
            WebApplicationBaseAddress = dataContract.WebApplicationBaseAddress,
            UseAzureKeyVault = dataContract.UseAzureKeyVault,
            AzureKeyVaultName = dataContract.AzureKeyVaultName,
            PollIntervalOverride = dataContract.PollIntervalOverride,
            AllowDisplayScripts = dataContract.AllowDisplayScripts,
            EnableTimeMachine = dataContract.EnableTimeMachine,
            TimelineDurationDays = dataContract.TimelineDurationDays
        };
    }
}