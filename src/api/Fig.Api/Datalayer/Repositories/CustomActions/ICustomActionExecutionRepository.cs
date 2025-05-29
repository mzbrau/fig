using System;
using System.Collections.Generic;
using Fig.Datalayer.BusinessEntities.CustomActions;

namespace Fig.Api.Datalayer.Repositories.CustomActions
{
    public interface ICustomActionExecutionRepository
    {
        CustomActionExecutionBusinessEntity? GetById(Guid id);
        void Add(CustomActionExecutionBusinessEntity entity);
        void Update(CustomActionExecutionBusinessEntity entity);
        IEnumerable<CustomActionExecutionBusinessEntity> GetHistory(Guid customActionId, int limit, int offset);
    }
}
