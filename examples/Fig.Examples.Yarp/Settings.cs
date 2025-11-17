using Fig.Client;
using Fig.Client.Abstractions.Attributes;
using Yarp.ReverseProxy.Configuration;

namespace Fig.Examples.Yarp;

public class Settings : SettingsBase
{
    public override string ClientDescription => "Simple YARP gateway example - configure routes and clusters for the reverse proxy";

    [Setting("YARP Configurtion")]
    [ConfigurationSectionOverride("ReverseProxy")]
    public ReverseProxy ReverseProxy { get; set; }
    public override IEnumerable<string> GetValidationErrors()
    {
        yield break;
    }
}

public class ReverseProxy
{
    public Dictionary<string, RouteConfig> Routes { get; set; }
    
    public Dictionary<string, ClusterConfig> Clusters { get; set; }
}