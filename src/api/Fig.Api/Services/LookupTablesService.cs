using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.LookupTable;

namespace Fig.Api.Services;

public class LookupTablesService : ILookupTablesService
{
    private readonly ILookupTableConverter _lookupTableConverter;
    private readonly ILookupTablesRepository _lookupTablesRepository;

    public LookupTablesService(ILookupTableConverter lookupTableConverter, ILookupTablesRepository lookupTablesRepository)
    {
        _lookupTableConverter = lookupTableConverter;
        _lookupTablesRepository = lookupTablesRepository;
    }
    
    public IEnumerable<LookupTableDataContract> Get()
    {
        var items = _lookupTablesRepository.GetAllItems();
        foreach (var item in items)
        {
            yield return _lookupTableConverter.Convert(item);
        }
    }

    public void Post(LookupTableDataContract item)
    {
        var businessEntity = _lookupTableConverter.Convert(item);
        _lookupTablesRepository.SaveItem(businessEntity);
    }

    public void Put(Guid id, LookupTableDataContract item)
    {
        var businessEntity = _lookupTablesRepository.GetItem(id);

        if (businessEntity != null)
        {
            businessEntity.Name = item.Name;
            businessEntity.LookupTable = item.LookupTable;

            _lookupTablesRepository.UpdateItem(businessEntity);
        }
        else
        {
            throw new KeyNotFoundException($"No lookup table with id {id}");
        }
    }

    public void Delete(Guid id)
    {
        var item = _lookupTablesRepository.GetItem(id);
        if (item != null)
            _lookupTablesRepository.DeleteItem(item);
    }
}