using Fig.Client.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientWithNestedSettings : TestSettingsBase
{
    public override string ClientDescription => "Client with nested settings";

    public override string ClientName => "ClientWithNestedSettings";
    
    [NestedSetting]
    public MessageBus? MessageBus { get; set; }
    
    [Setting("a timeout")]
    public double TimeoutMs { get; set; }
    
    [NestedSetting]
    public required Database Database { get; set; }
    
    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }
}

public class MessageBus
{
    [Setting("a uri")]
    public string? Uri { get; set; }
    
    [NestedSetting]
    public Authorization? Auth { get; set; }
}

public class Authorization
{
    [Setting("a user")]
    public string Username { get; set; } = "Frank";
    
    [Setting("a password")]
    public string? Password { get; set; }
}

public class Database
{
    [Setting("a connection string")]
    public string? ConnectionString { get; set; }
    
    [Setting("a timeout")]
    public int TimeoutMs { get; set; }
}