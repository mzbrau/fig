using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IClientStatusRepository
{
    ClientStatusBusinessEntity? GetClient(string name, string? instance = null);

    void UpdateClientStatus(ClientStatusBusinessEntity clientStatus);

    IList<ClientStatusBusinessEntity> GetAllClients(UserDataContract? requestingUser);
}