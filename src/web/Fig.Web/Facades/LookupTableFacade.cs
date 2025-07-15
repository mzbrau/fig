using Fig.Common.Events;
using Fig.Contracts.LookupTable;
using Fig.Web.Converters;
using Fig.Web.Events;
using Fig.Web.Models.LookupTables;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class LookupTableFacade : ILookupTablesFacade
{
    private const string LookupTablesRoute = "lookuptables";
    private readonly IHttpService _httpService;
    private readonly ILookupTableConverter _lookupTableConverter;

    public LookupTableFacade(IHttpService httpService, ILookupTableConverter lookupTableConverter,
        IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        _lookupTableConverter = lookupTableConverter;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () => { Items.Clear(); });
    }

    public List<LookupTable> Items { get; } = new();

    public async Task LoadAll()
    {
        var result = await _httpService.Get<List<LookupTableDataContract>>(LookupTablesRoute);

        if (result == null)
            return;

        Items.Clear();
        Items.AddRange(_lookupTableConverter.Convert(result));
    }

    public LookupTable CreateNew()
    {
        var newItem = new LookupTable("<New Lookup Table>", "1,example");

        Items.Add(newItem);
        return newItem;
    }

    public async Task<bool> Save(LookupTable item)
    {
        try
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
            return true;
        }
        catch
        {
            // Error has already been handled by HttpService and shown to user
            // Just return false to indicate failure
            return false;
        }
    }

    public async Task Delete(LookupTable item)
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