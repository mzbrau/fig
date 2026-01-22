using Fig.Api.Converters;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Enums;
using Fig.Api.Exceptions;
using Fig.Api.Validators;
using Fig.Contracts.LookupTable;

namespace Fig.Api.Services;

public class LookupTablesService : ILookupTablesService
{
    private readonly ILookupTableConverter _lookupTableConverter;
    private readonly ILookupTablesRepository _lookupTablesRepository;
    private readonly ISettingClientRepository _settingClientRepository;

    public LookupTablesService(ILookupTableConverter lookupTableConverter, ILookupTablesRepository lookupTablesRepository, ISettingClientRepository settingClientRepository)
    {
        _lookupTableConverter = lookupTableConverter;
        _lookupTablesRepository = lookupTablesRepository;
        _settingClientRepository = settingClientRepository;
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
        businessEntity.IsClientDefined = false;
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
            businessEntity.IsClientDefined = false;

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

    public async Task PostByClient(string clientName, string? clientSecret, LookupTableDataContract item)
    {
        await ValidateClient(clientName, clientSecret);
        
        // Check if a lookup table with the same name already exists
        var existingItem = await _lookupTablesRepository.GetItemByName(item.Name);
        if (existingItem is not null)
        {
            existingItem.LookupTable = item.LookupTable;
            existingItem.IsClientDefined = true;
            await _lookupTablesRepository.UpdateItem(existingItem);
        }
        else
        {
            var businessEntity = _lookupTableConverter.Convert(item);
            businessEntity.IsClientDefined = true;
            await _lookupTablesRepository.SaveItem(businessEntity);
        }
    }
    
    private async Task ValidateClient(string clientName, string? clientSecret)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new UnauthorizedAccessException("Client name is missing or empty.");
        
        // Use read-only since we only need to validate the client secret
        var client = await _settingClientRepository.GetClientReadOnly(clientName)
                     ?? throw new UnknownClientException(clientName);

        var registrationStatus = RegistrationStatusValidator.GetStatus(client, clientSecret!);
        if (registrationStatus == CurrentRegistrationStatus.DoesNotMatchSecret)
            throw new UnauthorizedAccessException("Invalid Secret");
    }
}