using Fig.Common.NetStandard.WebHook;
using Fig.Web.Models.WebHooks;

namespace Fig.Web.Converters;

public interface IWebHookClientConverter
{
    WebHookClientModel Convert(WebHookClientDataContract client);

    WebHookClientDataContract Convert(WebHookClientModel client);
}