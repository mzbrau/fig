using System;

namespace Fig.Contracts.SettingClients;

public class ClientSecretChangeResponseDataContract
{
    public ClientSecretChangeResponseDataContract(string clientName, DateTime oldSecretExpiryUtc)
    {
        ClientName = clientName;
        OldSecretExpiryUtc = oldSecretExpiryUtc;
    }

    public string ClientName { get; set; }

    public DateTime OldSecretExpiryUtc { get; set; }
}