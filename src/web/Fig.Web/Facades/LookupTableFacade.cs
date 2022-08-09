using Fig.Contracts.Common;
using Fig.Web.Converters;
using Fig.Web.Models.LookupTables;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class LookupTableFacade : ILookupTablesFacade
{
    private const string LookupTablesRoute = "lookuptables";
    private readonly IHttpService _httpService;
    private readonly ILookupTableConverter _lookupTableConverter;

    public LookupTableFacade(IHttpService httpService, ILookupTableConverter lookupTableConverter)
    {
        _httpService = httpService;
        _lookupTableConverter = lookupTableConverter;
    }

    public List<LookupTables> Items { get; private set; } = new();
    
    public async Task LoadAll()
    {
        var result = await _httpService.Get<List<LookupTableDataContract>>(LookupTablesRoute);

        if (result == null)
            return;

        Items.Clear();
        Items.AddRange(_lookupTableConverter.Convert(result));
    }

    public LookupTables CreateNew()
    {
        var newItem = new LookupTables()
        {
            Name = "<New Lookup Table>",
            LookupsAsText = "1,example"
        };

        Items.Add(newItem);
        return newItem;
    }

    public async Task Save(LookupTables item)
    {
        var dataContract = _lookupTableConverter.Convert(item);
        
        if (item.Id == null)
        {
            await _httpService.Post(LookupTablesRoute, dataContract);
        }
        else
        {
            await _httpService.Put($"{LookupTablesRoute}/{dataContract.Id}", dataContract);
        }

        await LoadAll();
    }

    public async Task Delete(LookupTables item)
    {
        if (item.Id == null)
        {
            Items.Remove(item);
            return;
        }
        
        var dataContract = _lookupTableConverter.Convert(item);
        await _httpService.Delete($"{LookupTablesRoute}/{dataContract.Id}");
        await LoadAll();
    }
}