using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Api.Datalayer.Repositories.CustomActions;
using Fig.Datalayer.BusinessEntities.CustomActions;
using NHibernate;
using NHibernate.Linq;

namespace Fig.Api.Datalayer.Repositories.CustomActions
{
    public class CustomActionExecutionResultRepository : RepositoryBase<CustomActionExecutionResultBusinessEntity>, ICustomActionExecutionResultRepository
    {
        public CustomActionExecutionResultRepository(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        public IEnumerable<CustomActionExecutionResultBusinessEntity> GetByExecutionId(Guid executionId)
        {
            return InSession(session =>
                session.Query<CustomActionExecutionResultBusinessEntity>()
                    .Where(x => x.CustomActionExecutionId == executionId)
                    .ToList());
        }
    }
}
