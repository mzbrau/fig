using Fig.Contracts.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IWebHookConverter
{
    WebHookDataContract Convert(WebHookBusinessEntity webHook);

    WebHookBusinessEntity Convert(WebHookDataContract webHook);
}