using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IVerificationHistoryRepository
{
    void Add(VerificationResultBusinessEntity result);

    IEnumerable<VerificationResultBusinessEntity> GetAll(Guid clientId, string verificationName);
}