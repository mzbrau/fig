using Fig.Contracts.Authentication;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IClientStatusRepository
{
    Task<ClientStatusBusinessEntity?> GetClient(string name, string? instance = null);
    
    /// <summary>
    /// Gets a client for read-only operations without acquiring a database lock.
    /// Use this method when you only need to read client data and won't modify it.
    /// </summary>
    Task<ClientStatusBusinessEntity?> GetClientReadOnly(string name, string? instance = null);

    Task UpdateClientStatus(ClientStatusBusinessEntity clientStatus);

    Task<IList<ClientStatusBusinessEntity>> GetAllClients(UserDataContract requestingUser);

    /// <summary>
    /// Gets client statuses by name. When <paramref name="instance"/> is null, returns all instances
    /// for that name; otherwise returns only the matching instance.
    /// </summary>
    Task<IList<ClientStatusBusinessEntity>> GetClients(
        string name, string? instance, UserDataContract requestingUser);
}