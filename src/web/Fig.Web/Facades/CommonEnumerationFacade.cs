using Fig.Contracts.Common;
using Fig.Web.Converters;
using Fig.Web.Models.CommonEnumerations;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class CommonEnumerationFacade : ICommonEnumerationFacade
{
    private const string CommonEnumerationsRoute = "commonenumerations";
    private readonly IHttpService _httpService;
    private readonly ICommonEnumerationConverter _commonEnumerationConverter;

    public CommonEnumerationFacade(IHttpService httpService, ICommonEnumerationConverter commonEnumerationConverter)
    {
        _httpService = httpService;
        _commonEnumerationConverter = commonEnumerationConverter;
    }

    public List<CommonEnumerationModel> Items { get; private set; } = new();
    
    public async Task LoadAll()
    {
        var result = await _httpService.Get<List<CommonEnumerationDataContract>>(CommonEnumerationsRoute);

        if (result == null)
            return;

        Items.Clear();
        Items.AddRange(_commonEnumerationConverter.Convert(result));
    }

    public CommonEnumerationModel CreateNew()
    {
        var newItem = new CommonEnumerationModel()
        {
            Name = "<NewEnumeration>"
        };

        Items.Add(newItem);
        return newItem;
    }

    public async Task Save(CommonEnumerationModel item)
    {
        var dataContract = _commonEnumerationConverter.Convert(item);
        
        if (item.Id == null)
        {
            await _httpService.Post(CommonEnumerationsRoute, dataContract);
        }
        else
        {
            await _httpService.Put($"{CommonEnumerationsRoute}/{dataContract.Id}", dataContract);
        }

        await LoadAll();
    }

    public async Task Delete(CommonEnumerationModel item)
    {
        if (item.Id == null)
        {
            Items.Remove(item);
            return;
        }
        
        var dataContract = _commonEnumerationConverter.Convert(item);
        await _httpService.Delete($"{CommonEnumerationsRoute}/{dataContract.Id}");
        await LoadAll();
    }
}