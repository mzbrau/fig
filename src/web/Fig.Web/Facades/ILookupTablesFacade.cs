using Fig.Web.Models.LookupTables;

namespace Fig.Web.Facades;

public interface ILookupTablesFacade
{
    List<LookupTables> Items { get; }

    Task LoadAll();

    LookupTables CreateNew();

    Task<bool> Save(LookupTables item);
    
    Task Delete(LookupTables selectedItem);
}