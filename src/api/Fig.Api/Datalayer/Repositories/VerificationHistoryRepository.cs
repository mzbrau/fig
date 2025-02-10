using System.Diagnostics;
using Fig.Api.Observability;
using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;
using ISession = NHibernate.ISession;

namespace Fig.Api.Datalayer.Repositories;

public class VerificationHistoryRepository : RepositoryBase<VerificationResultBusinessEntity>, IVerificationHistoryRepository
{
    public VerificationHistoryRepository(ISession session)
        : base(session)
    {
    }
    
    public async Task Add(VerificationResultBusinessEntity result)
    {
        await Save(result);
    }

    public async Task<IList<VerificationResultBusinessEntity>> GetAll(Guid clientId, string verificationName)
    {
        using Activity? activity = ApiActivitySource.Instance.StartActivity();
        var criteria = Session.CreateCriteria<VerificationResultBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(VerificationResultBusinessEntity.ClientId), clientId));
        criteria.Add(Restrictions.Eq(nameof(VerificationResultBusinessEntity.VerificationName), verificationName));
        criteria.AddOrder(Order.Desc(nameof(VerificationResultBusinessEntity.ExecutionTime)));
        return await criteria.ListAsync<VerificationResultBusinessEntity>();
    }
}