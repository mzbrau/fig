using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

/// <summary>
/// Combined settings for offline flow process E2E tests: nested settings, datagrid, and datagrid column secrets.
/// </summary>
public class OfflineFlowTestHostSettings : TestSettingsBase
{
    public override string ClientName => "OfflineFlowTestHost";

    public override string ClientDescription => "Offline flow test host settings";

    [NestedSetting]
    public ServiceConfig Service { get; set; } = new();

    [Setting("Accounts", defaultValueMethodName: nameof(GetDefaultAccounts))]
    public List<AccountEntry> Accounts { get; set; } = null!;

    [Secret]
    [Setting("Api Key")]
    public string ApiKey { get; set; } = "default-api-key";

    public override IEnumerable<string> GetValidationErrors() => [];

    public static List<AccountEntry> GetDefaultAccounts() =>
    [
        new() { Username = "alice", Password = "alice-secret" },
        new() { Username = "bob", Password = "bob-secret" },
    ];
}

public class ServiceConfig
{
    [Setting("Endpoint")]
    public string Endpoint { get; set; } = "https://api.example.com";

    [NestedSetting]
    public ServiceAuth Auth { get; set; } = new();
}

public class ServiceAuth
{
    [Setting("Username")]
    public string Username { get; set; } = "service-user";

    [Secret]
    [Setting("Token")]
    public string Token { get; set; } = "service-token";
}

public class AccountEntry
{
    public string Username { get; set; } = string.Empty;

    [Secret]
    public string Password { get; set; } = string.Empty;
}
