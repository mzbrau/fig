using System.Collections.Generic;
using Fig.Client.Abstractions.Attributes;

namespace Fig.Test.Common.TestSettings;

public class ClientWithComplexConfigurationSections : TestSettingsBase
{
    public override string ClientName => "ClientWithComplexConfigurationSections";

    public override string ClientDescription => "ClientWithComplexConfigurationSections";

    [NestedSetting]
    public ComplexDatabaseSettings Database { get; set; } = new();

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class ComplexDatabaseSettings
{
    [Setting("Complex database connection overrides", true, nameof(GetDefaultConnections))]
    [ConfigurationSectionOverride("ConnectionOverrides", "ReplicaConnections")]
    public List<ComplexConnection> Connections { get; set; } = GetDefaultConnections();

    public static List<ComplexConnection> GetDefaultConnections() =>
    [
        new()
        {
            UserName = "primary-user",
            Password = "primary-password",
        },
        new()
        {
            UserName = "secondary-user",
            Password = "secondary-password",
        },
    ];
}

public class ComplexConnection
{
    public string UserName { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;
}
