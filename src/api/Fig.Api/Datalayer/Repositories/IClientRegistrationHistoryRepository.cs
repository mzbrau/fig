using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IClientRegistrationHistoryRepository
{
    Task Add(ClientRegistrationHistoryBusinessEntity history);

    Task<IList<ClientRegistrationHistoryBusinessEntity>> GetAll();

    Task ClearAll();
}
