using System;
using System.Collections.Generic;
using Fig.Datalayer.BusinessEntities.CustomActions;

namespace Fig.Api.Datalayer.Repositories.CustomActions
{
    public interface ICustomActionRepository
    {
        CustomActionBusinessEntity? GetById(Guid id);
        IEnumerable<CustomActionBusinessEntity> GetByClientId(Guid clientId);
        void Add(CustomActionBusinessEntity entity);
        void Update(CustomActionBusinessEntity entity);
        void Delete(CustomActionBusinessEntity entity);
        IEnumerable<CustomActionExecutionBusinessEntity> GetAllPending(string clientName, string? instance);
        CustomActionExecutionBusinessEntity? GetExecutionById(Guid executionId);
        void AddExecution(CustomActionExecutionBusinessEntity executionEntity);
        void UpdateExecution(CustomActionExecutionBusinessEntity executionEntity);
    }
}
