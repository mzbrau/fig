using System.Text.RegularExpressions;
using Fig.Client.Abstractions.Data;
using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class UserDataContractExtensionMethods
{
    public static bool HasAccess(this UserDataContract user, string clientName)
    {
        var filter = user.ClientFilter;
        if (string.IsNullOrWhiteSpace(filter))
            filter = ".*";
        
        return Regex.IsMatch(clientName, filter);
    }
    
    public static bool HasPermissionForClassification(this UserDataContract? user, SettingBusinessEntity setting)
    {
        return user.HasPermissionForClassification(setting.Classification);
    }

    public static bool HasPermissionForClassification(this UserDataContract? user, CustomActionBusinessEntity customAction)
    {
        return user.HasPermissionForClassification(customAction.Classification);
    }

    public static bool HasPermissionForClassification(this UserDataContract? user, Classification classification)
    {
        return user?.AllowedClassifications.Contains(classification) == true;
    }
}
