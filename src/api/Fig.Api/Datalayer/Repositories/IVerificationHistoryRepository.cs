using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Datalayer.Repositories;

public interface IVerificationHistoryRepository
{
    void Add(VerificationResultBusinessEntity result);

    IList<VerificationResultBusinessEntity> GetAll(Guid clientId, string verificationName);
}