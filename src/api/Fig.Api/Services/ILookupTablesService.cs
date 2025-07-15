using Fig.Contracts.LookupTable;

namespace Fig.Api.Services;

public interface ILookupTablesService
{
    Task<IEnumerable<LookupTableDataContract>> Get();

    Task Post(LookupTableDataContract item);

    Task Put(Guid id, LookupTableDataContract item);

    Task Delete(Guid item);
    
    Task PostByClient(string clientName, string? clientSecret, LookupTableDataContract item);
}