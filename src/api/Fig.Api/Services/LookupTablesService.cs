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
    
    public async Task<IEnumerable<LookupTableDataContract>> Get()
    {
        var items = await _lookupTablesRepository.GetAllItems();
        return items.Select(item => _lookupTableConverter.Convert(item)).ToList();
    }

    public async Task Post(LookupTableDataContract item)
    {
        // Check if a lookup table with the same name already exists
        var existingItem = await _lookupTablesRepository.GetItemByName(item.Name);
        if (existingItem != null)
        {
            throw new InvalidOperationException($"A lookup table with the name '{item.Name}' already exists.");
        }

        var businessEntity = _lookupTableConverter.Convert(item);
        await _lookupTablesRepository.SaveItem(businessEntity);
    }

    public async Task Put(Guid id, LookupTableDataContract item)
    {
        var businessEntity = await _lookupTablesRepository.GetItem(id);

        if (businessEntity != null)
        {
            // Check if a different lookup table with the same name already exists
            var existingItemWithSameName = await _lookupTablesRepository.GetItemByName(item.Name);
            if (existingItemWithSameName != null && existingItemWithSameName.Id != id)
            {
                throw new InvalidOperationException($"A lookup table with the name '{item.Name}' already exists.");
            }

            businessEntity.Name = item.Name;
            businessEntity.LookupTable = item.LookupTable;

            await _lookupTablesRepository.UpdateItem(businessEntity);
        }
        else
        {
            throw new KeyNotFoundException($"No lookup table with id {id}");
        }
    }

    public async Task Delete(Guid id)
    {
        var item = await _lookupTablesRepository.GetItem(id);
        if (item != null)
            await _lookupTablesRepository.DeleteItem(item);
    }
}