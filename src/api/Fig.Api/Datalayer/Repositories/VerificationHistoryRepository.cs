using Fig.Datalayer.BusinessEntities;
using NHibernate.Criterion;

namespace Fig.Api.Datalayer.Repositories;

public class VerificationHistoryRepository : RepositoryBase<VerificationResultBusinessEntity>, IVerificationHistoryRepository
{
    public VerificationHistoryRepository(IFigSessionFactory sessionFactory)
        : base(sessionFactory)
    {
    }
    
    public void Add(VerificationResultBusinessEntity result)
    {
        Save(result);
    }

    public IEnumerable<VerificationResultBusinessEntity> GetAll(Guid clientId, string verificationName)
    {
        using var session = SessionFactory.OpenSession();
        var criteria = session.CreateCriteria<VerificationResultBusinessEntity>();
        criteria.Add(Restrictions.Eq(nameof(VerificationResultBusinessEntity.ClientId), clientId));
        criteria.Add(Restrictions.Eq(nameof(VerificationResultBusinessEntity.VerificationName), verificationName));
        criteria.AddOrder(Order.Desc(nameof(VerificationResultBusinessEntity.ExecutionTime)));
        return criteria.List<VerificationResultBusinessEntity>();
    }
}