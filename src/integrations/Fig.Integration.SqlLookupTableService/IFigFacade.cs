namespace Fig.Integration.SqlLookupTableService;

public interface IFigFacade
{
    Task Login();
    
    Task UpdateLookup(LookupTableConfiguration lookupTableConfiguration, Dictionary<string, string> lookupDetails);

    Task GetExistingLookups();
}