using System.Collections.Generic;

namespace Fig.Contracts.ClientRegistrationHistory;

public class ClientRegistrationHistoryCollectionDataContract
{
    public ClientRegistrationHistoryCollectionDataContract()
    {
    }

    public ClientRegistrationHistoryCollectionDataContract(List<ClientRegistrationHistoryDataContract> registrations)
    {
        Registrations = registrations;
    }

    public List<ClientRegistrationHistoryDataContract> Registrations { get; set; } = new();
}
