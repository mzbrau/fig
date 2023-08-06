using Fig.Contracts.WebHook;
using Fig.Web.Models.WebHooks;

namespace Fig.Web.Converters;

public class WebHookClientConverter : IWebHookClientConverter
{
    public WebHookClientModel Convert(WebHookClientDataContract client)
    {
        return new WebHookClientModel
        {
            Id = client.Id,
            Name = client.Name,
            BaseUri = client.BaseUri,
            Secret = client.Secret,
            UpdateSecret = false
        };
    }

    public WebHookClientDataContract Convert(WebHookClientModel client)
    {
        return new WebHookClientDataContract(client.Id, client.Name!, client.BaseUri!, client.Secret);
    }
}