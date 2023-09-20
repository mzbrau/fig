using System.Text.RegularExpressions;
using Fig.Contracts.Authentication;

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
}