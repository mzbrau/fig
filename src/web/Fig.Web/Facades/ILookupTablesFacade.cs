using Fig.Web.Models.LookupTables;

namespace Fig.Web.Facades;

public interface ILookupTablesFacade
{
    List<LookupTable> Items { get; }

    Task LoadAll();

    LookupTable CreateNew();

    LookupTable CreateDraft(string name, string? lookupsAsText = null);

    Task<bool> Save(LookupTable item);
    
    Task Delete(LookupTable selectedItem);
}