using Fig.Contracts.LookupTable;
using Fig.Integration.SqlLookupTableService.ExtensionMethods;

namespace Fig.Integration.SqlLookupTableService;

public class FigFacade : IFigFacade
{
    private const string LookupTablesRoute = "lookuptables";
    private readonly IHttpService _httpService;
    private List<LookupTableDataContract>? _previousLookupDetails = new();

    public FigFacade(IHttpService httpService)
    {
        _httpService = httpService;
    }
    
    public async Task UpdateLookup(LookupTableConfiguration configuration,
        Dictionary<string, string> lookupDetails)
    {
        var existing = _previousLookupDetails?.FirstOrDefault(a => a.Name == configuration.Name);
        if (existing != null)
        {
            // Convert existing lookup table to non-nullable for comparison
            var existingNonNullable = existing.LookupTable.ToDictionary(kvp => kvp.Key, kvp => kvp.Value ?? string.Empty);
            if (lookupDetails.ContentEquals(existingNonNullable))
                return;
        }
        
        // Convert to nullable dictionary for the contract
        var nullableLookupDetails = lookupDetails.ToDictionary(kvp => kvp.Key, kvp => (string?)kvp.Value);
        var dataContract = new LookupTableDataContract(existing?.Id, configuration.Name ?? string.Empty, nullableLookupDetails);
        if (existing == null)
        {
            await _httpService.Post(LookupTablesRoute, dataContract);
        }
        else
        {
            await _httpService.Put($"{LookupTablesRoute}/{existing.Id}", dataContract);
        }
    }

    public async Task GetExistingLookups()
    {
        _previousLookupDetails = await _httpService.Get<List<LookupTableDataContract>>(LookupTablesRoute);
    }

    public async Task Login()
    {
        await _httpService.LogIn();
    }
}