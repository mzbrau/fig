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
            AllowDynamicVerifications = model.AllowDynamicVerifications
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
            AllowDynamicVerifications = dataContract.AllowDynamicVerifications
        };
    }
}