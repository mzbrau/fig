using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories
{
    public class CustomActionRepository : RepositoryBase<CustomActionBusinessEntity>, ICustomActionRepository
    {
        public CustomActionRepository(ISession session)
            : base(session)
        {
        }

        public async Task<CustomActionBusinessEntity?> GetByName(string clientName, string name)
        {
            using Activity? activity = ApiActivitySource.Instance.StartActivity();
            var criteria = Session.CreateCriteria<CustomActionBusinessEntity>();
            criteria.Add(Restrictions.Eq(nameof(CustomActionBusinessEntity.ClientName), clientName));
            criteria.Add(Restrictions.Eq(nameof(CustomActionBusinessEntity.Name), name));
            var customAction = await criteria.UniqueResultAsync<CustomActionBusinessEntity>();
            return customAction;
        }

        public async Task<IEnumerable<CustomActionBusinessEntity>> GetByClientName(string clientName)
        {
            using Activity? activity = ApiActivitySource.Instance.StartActivity();
            var criteria = Session.CreateCriteria<CustomActionBusinessEntity>();
            criteria.Add(Restrictions.Eq(nameof(CustomActionBusinessEntity.ClientName), clientName));
            return await criteria.ListAsync<CustomActionBusinessEntity>();
        }

        public async Task AddCustomAction(CustomActionBusinessEntity entity)
        {
            await Save(entity);
        }

        public async Task UpdateCustomAction(CustomActionBusinessEntity entity)
        {
            await Update(entity);
        }

        public async Task DeleteAllForClient(string clientName)
        {
            using Activity? activity = ApiActivitySource.Instance.StartActivity();
            var criteria = Session.CreateCriteria<CustomActionBusinessEntity>();
            criteria.Add(Restrictions.Eq(nameof(CustomActionBusinessEntity.ClientName), clientName));
            var actions = await criteria.ListAsync<CustomActionBusinessEntity>();
            foreach (var action in actions)
            {
                await Delete(action);
            }
        }

        public async Task DeleteCustomAction(CustomActionBusinessEntity actionToRemove)
        {
            await Delete(actionToRemove);
        }
    }
}
