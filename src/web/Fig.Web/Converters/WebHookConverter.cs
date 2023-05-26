using Fig.Common.NetStandard.WebHook;
using Fig.Web.Models.WebHooks;

namespace Fig.Web.Converters;

public class WebHookConverter : IWebHookConverter
{
    public WebHookModel Convert(WebHookDataContract webHook)
    {
        return new WebHookModel
        {
            Id = webHook.Id,
            ClientId = webHook.ClientId,
            WebHookType = webHook.WebHookType,
            ClientNameRegex = webHook.ClientNameRegex,
            SettingNameRegex = webHook.SettingNameRegex,
            MinSessions = webHook.MinSessions
        };
    }

    public WebHookDataContract Convert(WebHookModel webHook)
    {
        return new WebHookDataContract(webHook.Id, webHook.ClientId, webHook.WebHookType, webHook.ClientNameRegex,
            webHook.SettingNameRegex, webHook.MinSessions);
    }
}