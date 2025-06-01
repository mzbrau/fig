using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories
{
    public class CustomActionExecutionRepository : RepositoryBase<CustomActionExecutionBusinessEntity>, ICustomActionExecutionRepository
    {
        public CustomActionExecutionRepository(ISession session) 
            : base(session)
        {
        }

        public async Task<CustomActionExecutionBusinessEntity?> GetById(Guid id)
        {
            using Activity? activity = ApiActivitySource.Instance.StartActivity();
            var criteria = Session.CreateCriteria<CustomActionExecutionBusinessEntity>();
            criteria.Add(Restrictions.Eq(nameof(CustomActionExecutionBusinessEntity.Id), id));
            criteria.SetLockMode(LockMode.Upgrade);
            var execution = await criteria.UniqueResultAsync<CustomActionExecutionBusinessEntity>();
            return execution;
        }

        public async Task AddExecutionRequest(CustomActionExecutionBusinessEntity entity)
        {
            await Save(entity);
        }

        public async Task UpdateExecution(CustomActionExecutionBusinessEntity entity)
        {
            await Update(entity);
        }

        public async Task<IEnumerable<CustomActionExecutionBusinessEntity>> GetHistory(string clientName, string customActionName, DateTime startDate, DateTime endDate)
        {
            using Activity? activity = ApiActivitySource.Instance.StartActivity();
            var criteria = Session.CreateCriteria<CustomActionExecutionBusinessEntity>();
            criteria.Add(Restrictions.Ge(nameof(CustomActionExecutionBusinessEntity.ExecutedAt), startDate));
            criteria.Add(Restrictions.Lt(nameof(CustomActionExecutionBusinessEntity.ExecutedAt), endDate));
            criteria.Add(Restrictions.Eq(nameof(CustomActionExecutionBusinessEntity.ClientName), clientName));
            criteria.Add(Restrictions.Eq(nameof(CustomActionExecutionBusinessEntity.CustomActionName), customActionName));
            criteria.AddOrder(Order.Desc(nameof(CustomActionExecutionBusinessEntity.RequestedAt)));
            return await criteria.ListAsync<CustomActionExecutionBusinessEntity>();  
        }

        public async Task<IEnumerable<CustomActionExecutionBusinessEntity>> GetAllPending(string clientName)
        {
            using Activity? activity = ApiActivitySource.Instance.StartActivity();
            var criteria = Session.CreateCriteria<CustomActionExecutionBusinessEntity>();
            criteria.Add(Restrictions.Eq(nameof(CustomActionExecutionBusinessEntity.ClientName), clientName));
            criteria.Add(Restrictions.IsNull(nameof(CustomActionExecutionBusinessEntity.HandlingInstance)));
            return await criteria.ListAsync<CustomActionExecutionBusinessEntity>();  
        }
        
    }
}
