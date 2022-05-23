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
        businessEntity.AllowDynamicVerifications = dataContract.AllowDynamicVerifications;
    }
}