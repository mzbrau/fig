using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IClientStatusRepository
{
    Task<ClientStatusBusinessEntity?> GetClient(string name, string? instance = null);

    Task UpdateClientStatus(ClientStatusBusinessEntity clientStatus);

    Task DeleteClient(ClientStatusBusinessEntity clientStatus);

    Task<IList<ClientStatusBusinessEntity>> GetAllClients(UserDataContract? requestingUser);
}