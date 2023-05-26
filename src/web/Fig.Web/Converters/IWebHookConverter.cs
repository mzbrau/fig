using Fig.Common.NetStandard.WebHook;
using Fig.Web.Models.WebHooks;

namespace Fig.Web.Converters;

public interface IWebHookConverter
{
    WebHookModel Convert(WebHookDataContract webHook);

    WebHookDataContract Convert(WebHookModel webHook);
}