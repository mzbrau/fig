using System;

namespace Fig.Common.WebHook;

public class WebHookClientDataContract
{
    public Guid? Id { get; set; }
    
    public string Name { get; set; }
    
    public Uri BaseUri { get; set; }
    
    public string? HashedSecret { get; set; }
}