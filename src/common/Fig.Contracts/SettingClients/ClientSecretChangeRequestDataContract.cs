using System;

namespace Fig.Contracts.SettingClients;

public class ClientSecretChangeRequestDataContract
{
    public ClientSecretChangeRequestDataContract(string newSecret, DateTime oldSecretExpiryUtc)
    {
        NewSecret = newSecret;
        OldSecretExpiryUtc = oldSecretExpiryUtc;
    }

    public string NewSecret { get; set; }
    
    public DateTime OldSecretExpiryUtc { get; set; }
}