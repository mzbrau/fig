using Fig.Client.Attributes;
using Microsoft.Extensions.Logging;

namespace Fig.Test.Common.TestSettings;

public class SecretSettings : TestSettingsBase
{
    public override string ClientName => "SecretClient";
    public override string ClientDescription => "Client with secret settings;";

    [Setting("Not a secret")]
    public string? NoSecret { get; set; }

    [Secret]
    [Setting("Secret with default")]
    public string? SecretWithDefault { get; set; } = "cat";

    [Secret]
    [Setting("Secret no default")]
    public string? SecretNoDefault { get; set; }
    
    [Setting("Secret data grid")]
    public List<Login>? Logins { get; set; }
    
    [Setting("Secret data grid with defaults", defaultValueMethodName: nameof(GetDefaultLogins))]
    public required List<Login> LoginsWithDefault { get; set; }

    public override IEnumerable<string> GetValidationErrors()
    {
        return [];
    }

    public static List<Login> GetDefaultLogins()
    {
        return
        [
            new()
            {
                Username = "myUser",
                Password = "myPassword"
            },

            new()
            {
                Username = "myUser2",
                Password = "myPassword2"
            }
        ];
    }
}

public class Login
{
    public string? Username { get; set; }
    
    [Secret]
    public string? Password { get; set; }
    
    [Secret]
    public string? AnotherSecret { get; set; }
}