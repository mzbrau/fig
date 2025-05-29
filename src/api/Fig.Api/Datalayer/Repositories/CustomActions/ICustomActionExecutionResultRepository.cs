using System;
using System.Collections.Generic;
using Fig.Datalayer.BusinessEntities.CustomActions;

namespace Fig.Api.Datalayer.Repositories.CustomActions
{
    public interface ICustomActionExecutionResultRepository
    {
        void Add(CustomActionExecutionResultBusinessEntity entity);
        IEnumerable<CustomActionExecutionResultBusinessEntity> GetByExecutionId(Guid executionId);
    }
}
