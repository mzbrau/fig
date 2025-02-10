using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IVerificationHistoryRepository
{
    Task Add(VerificationResultBusinessEntity result);

    Task<IList<VerificationResultBusinessEntity>> GetAll(Guid clientId, string verificationName);
}