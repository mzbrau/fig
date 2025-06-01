using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories
{
    public interface ICustomActionRepository
    {
        Task<CustomActionBusinessEntity?> GetByName(string clientName, string name);
        
        Task<IEnumerable<CustomActionBusinessEntity>> GetByClientName(string clientName);
        
        Task AddCustomAction(CustomActionBusinessEntity entity);
        
        Task UpdateCustomAction(CustomActionBusinessEntity entity);
        
        Task DeleteAllForClient(string clientName);
        
        Task DeleteCustomAction(CustomActionBusinessEntity actionToRemove);
    }
}
