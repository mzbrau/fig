using Fig.Common.NetStandard.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public interface IWebHookClientConverter
{
    WebHookClientDataContract Convert(WebHookClientBusinessEntity client);

    WebHookClientBusinessEntity Convert(WebHookClientDataContract client);
}