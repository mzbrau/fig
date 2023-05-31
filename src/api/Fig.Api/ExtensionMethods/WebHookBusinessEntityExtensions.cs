using System.Text.RegularExpressions;
using Fig.Api.Utils;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class WebHookBusinessEntityExtensions
{
    public static bool IsMatch(this WebHookBusinessEntity webHook, SettingClientBusinessEntity client)
    {
        return Regex.IsMatch(client.Name, webHook.ClientNameRegex);
    }
    
    public static bool IsMatch(this WebHookBusinessEntity webHook, ClientStatusBusinessEntity client)
    {
        return Regex.IsMatch(client.Name, webHook.ClientNameRegex);
    }
    
    public static bool IsMatch(this WebHookBusinessEntity webHook, ChangedSetting setting)
    {
        if (string.IsNullOrWhiteSpace(webHook.SettingNameRegex))
        {
            return false;
        }
        
        return Regex.IsMatch(setting.Name, webHook.SettingNameRegex);
    }
}