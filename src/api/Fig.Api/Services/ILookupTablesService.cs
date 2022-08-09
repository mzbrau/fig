using Fig.Contracts.Common;

namespace Fig.Api.Services;

public interface ILookupTablesService
{
    IEnumerable<LookupTableDataContract> Get();

    void Post(LookupTableDataContract item);

    void Put(Guid id, LookupTableDataContract item);

    void Delete(Guid item);
}