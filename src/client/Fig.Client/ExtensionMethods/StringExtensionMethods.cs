using Fig.Common.NetStandard.IpAddress;
using Fig.Common.NetStandard;
using System;

namespace Fig.Client.ExtensionMethods;

public static class StringExtensionMethods
{
    public static string? ReplaceConstants(this string? value, IIpAddressResolver ipAddressResolver)
    {
        if (value == Constants.EnumNullPlaceholder)
        {
            return null;
        }
        
        return value?.Replace(SettingConstants.MachineName, Environment.MachineName)
            .Replace(SettingConstants.User, Environment.UserName)
            .Replace(SettingConstants.Domain, Environment.UserDomainName)
            .Replace(SettingConstants.IpAddress, ipAddressResolver.Resolve())
            .Replace(SettingConstants.ProcessorCount, $"{Environment.ProcessorCount}")
            .Replace(SettingConstants.OsVersion, Environment.OSVersion.VersionString);
    }
}