using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class WebHookConverter : IWebHookConverter
{
    public WebHookDataContract Convert(WebHookBusinessEntity webHook)
    {
        return new WebHookDataContract(webHook.Id, webHook.ClientId, webHook.WebHookType, webHook.ClientNameRegex,
            webHook.SettingNameRegex, webHook.MinSessions);
    }

    public WebHookBusinessEntity Convert(WebHookDataContract webHook)
    {
        var result = new WebHookBusinessEntity
        {
            WebHookType = webHook.WebHookType,
            ClientId = webHook.ClientId,
            ClientNameRegex = webHook.ClientNameRegex,
            SettingNameRegex = webHook.SettingNameRegex,
            MinSessions = webHook.MinSessions
        };

        if (webHook.Id is not null)
            result.Id = webHook.Id;

        return result;
    }
}