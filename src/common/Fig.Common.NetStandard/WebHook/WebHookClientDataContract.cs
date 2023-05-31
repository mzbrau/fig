using System;

namespace Fig.Common.NetStandard.WebHook;

public class WebHookClientDataContract
{
    public WebHookClientDataContract(Guid? id, string name, Uri baseUri, string secret)
    {
        Id = id;
        Name = name;
        BaseUri = baseUri;
        Secret = secret;
    }

    public Guid? Id { get; set; }
    
    public string Name { get; set; }
    
    public Uri BaseUri { get; set; }
    
    public string Secret { get; set; }
}