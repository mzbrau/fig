using System.Collections.Generic;

namespace Fig.Contracts.SettingClients
{
    public class ClientsDescriptionDataContract
    {
        public ClientsDescriptionDataContract(IEnumerable<ClientDescriptionDataContract> clients)
        {
            Clients = clients;
        }

        public IEnumerable<ClientDescriptionDataContract> Clients { get; }
    }
}
