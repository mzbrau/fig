using Fig.Common.WebHook;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class WebHookClientConverter : IWebHookClientConverter
{
    public WebHookClientDataContract Convert(WebHookClientBusinessEntity client)
    {
        return new WebHookClientDataContract
        {
            Id = client.Id,
            Name = client.Name,
            BaseUri = new Uri(client.BaseUri),
            HashedSecret = client.HashedSecret
        };
    }

    public WebHookClientBusinessEntity Convert(WebHookClientDataContract client)
    {
        var businessEntity = new WebHookClientBusinessEntity
        {
            Name = client.Name,
            BaseUri = client.BaseUri.ToString(),
        };

        if (client.Id is not null)
            businessEntity.Id = client.Id.Value;

        return businessEntity;
    }
}