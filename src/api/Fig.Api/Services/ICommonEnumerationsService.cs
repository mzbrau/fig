using Fig.Contracts.Common;

namespace Fig.Api.Services;

public interface ICommonEnumerationsService
{
    IEnumerable<CommonEnumerationDataContract> Get();

    void Post(CommonEnumerationDataContract item);

    void Put(Guid id, CommonEnumerationDataContract item);

    void Delete(Guid item);
}