using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.Common;

namespace Fig.Api.Services;

public class CommonEnumerationsService : ICommonEnumerationsService
{
    private readonly ICommonEnumerationConverter _commonEnumerationConverter;
    private readonly ICommonEnumerationsRepository _commonEnumerationsRepository;

    public CommonEnumerationsService(ICommonEnumerationConverter commonEnumerationConverter, ICommonEnumerationsRepository commonEnumerationsRepository)
    {
        _commonEnumerationConverter = commonEnumerationConverter;
        _commonEnumerationsRepository = commonEnumerationsRepository;
    }
    
    public IEnumerable<CommonEnumerationDataContract> Get()
    {
        var items = _commonEnumerationsRepository.GetAllItems();
        foreach (var item in items)
        {
            yield return _commonEnumerationConverter.Convert(item);
        }
    }

    public void Post(CommonEnumerationDataContract item)
    {
        var businessEntity = _commonEnumerationConverter.Convert(item);
        _commonEnumerationsRepository.SaveItem(businessEntity);
    }

    public void Put(Guid id, CommonEnumerationDataContract item)
    {
        var businessEntity = _commonEnumerationsRepository.GetItem(id);

        if (businessEntity != null)
        {
            businessEntity.Name = item.Name;
            businessEntity.Enumeration = item.Enumeration;

            _commonEnumerationsRepository.UpdateItem(businessEntity);
        }
        else
        {
            throw new KeyNotFoundException($"No enumeration with id {id}");
        }
    }

    public void Delete(Guid id)
    {
        var item = _commonEnumerationsRepository.GetItem(id);
        if (item != null)
            _commonEnumerationsRepository.DeleteItem(item);
    }
}