using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Api.Datalayer.Repositories.CustomActions;
using Fig.Datalayer.BusinessEntities.CustomActions;
using NHibernate;
using NHibernate.Linq;

namespace Fig.Api.Datalayer.Repositories.CustomActions
{
    public class CustomActionExecutionRepository : RepositoryBase<CustomActionExecutionBusinessEntity>, ICustomActionExecutionRepository
    {
        public CustomActionExecutionRepository(ISessionFactory sessionFactory) : base(sessionFactory)
        {
        }

        public IEnumerable<CustomActionExecutionBusinessEntity> GetHistory(Guid customActionId, int limit, int offset)
        {
            return InSession(session =>
                session.Query<CustomActionExecutionBusinessEntity>()
                    .Where(x => x.CustomActionId == customActionId)
                    .OrderByDescending(x => x.RequestedAt)
                    .Skip(offset)
                    .Take(limit)
                    .ToList());
        }
    }
}
