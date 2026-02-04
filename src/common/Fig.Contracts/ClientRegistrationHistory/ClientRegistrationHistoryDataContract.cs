using System;
using System.Collections.Generic;

namespace Fig.Contracts.ClientRegistrationHistory;

public class ClientRegistrationHistoryDataContract
{
    public ClientRegistrationHistoryDataContract()
    {
    }

    public ClientRegistrationHistoryDataContract(
        Guid id,
        DateTime registrationDateUtc,
        string clientName,
        string clientVersion,
        List<SettingDefaultValueDataContract> settings)
    {
        Id = id;
        RegistrationDateUtc = registrationDateUtc;
        ClientName = clientName;
        ClientVersion = clientVersion;
        Settings = settings;
    }

    public Guid Id { get; set; }

    public DateTime RegistrationDateUtc { get; set; }

    public string ClientName { get; set; } = string.Empty;

    public string ClientVersion { get; set; } = string.Empty;

    public List<SettingDefaultValueDataContract> Settings { get; set; } = new();
}
