using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories
{
    public interface ICustomActionExecutionRepository
    {
        Task<CustomActionExecutionBusinessEntity?> GetById(Guid id);
        
        Task AddExecutionRequest(CustomActionExecutionBusinessEntity entity);
        
        Task UpdateExecution(CustomActionExecutionBusinessEntity entity);

        Task<IEnumerable<CustomActionExecutionBusinessEntity>> GetHistory(string clientName, string customActionName, DateTime startDate,
            DateTime endDate);

        Task<IEnumerable<CustomActionExecutionBusinessEntity>> GetAllPending(string clientName);
    }
}
