using Fig.Common.NetStandard.WebHook;
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
            HashedSecret = client.HashedSecret,
        };
    }

    public WebHookClientDataContract Convert(WebHookClientModel client)
    {
        var hashedSecret = client.HashedSecret;
        if (client.Secret != null)
        {
            hashedSecret = BCrypt.Net.BCrypt.EnhancedHashPassword(client.Secret);
        }

        return new WebHookClientDataContract(client.Id, client.Name, client.BaseUri, hashedSecret);
    }
}