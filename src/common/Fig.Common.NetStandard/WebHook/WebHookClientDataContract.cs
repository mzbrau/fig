using System;

namespace Fig.Common.NetStandard.WebHook;

public class WebHookClientDataContract
{
    public WebHookClientDataContract(Guid? id, string name, Uri baseUri, string? hashedSecret)
    {
        Id = id;
        Name = name;
        BaseUri = baseUri;
        HashedSecret = hashedSecret;
    }

    public Guid? Id { get; set; }
    
    public string Name { get; set; }
    
    public Uri BaseUri { get; set; }
    
    public string? HashedSecret { get; set; }
}