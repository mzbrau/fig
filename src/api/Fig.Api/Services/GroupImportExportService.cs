using Fig.Api.Datalayer.Repositories;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Services;

public class GroupImportExportService : AuthenticatedService, IGroupImportExportService
{
    private readonly ISettingGroupRepository _settingGroupRepository;
    private readonly IEventLogRepository _eventLogRepository;
    private readonly IEventLogFactory _eventLogFactory;
    private readonly ILogger<GroupImportExportService> _logger;

    public GroupImportExportService(
        ISettingGroupRepository settingGroupRepository,
        IEventLogRepository eventLogRepository,
        IEventLogFactory eventLogFactory,
        ILogger<GroupImportExportService> logger)
    {
        _settingGroupRepository = settingGroupRepository;
        _eventLogRepository = eventLogRepository;
        _eventLogFactory = eventLogFactory;
        _logger = logger;
    }

    public async Task<SettingGroupExportDataContract> ExportGroups()
    {
        var entities = await _settingGroupRepository.GetAllGroups();
        var groups = entities.Select(ConvertToDataContract).ToList();

        await _eventLogRepository.Add(_eventLogFactory.DataExported(AuthenticatedUser));

        return new SettingGroupExportDataContract(DateTime.UtcNow, 1, groups);
    }

    public async Task<ImportResultDataContract> ImportGroups(SettingGroupExportDataContract data, ImportType importType)
    {
        ValidateImport(data, importType);

        try
        {
            switch (importType)
            {
                case ImportType.ClearAndImport:
                    await ClearAndImport(data);
                    break;
                case ImportType.AddNew:
                    await AddNew(data);
                    break;
                case ImportType.ReplaceExisting:
                    await ReplaceExisting(data);
                    break;
            }

            await _eventLogRepository.Add(
                _eventLogFactory.DataImported(importType, DataImport.ImportMode.Api, data.Groups.Count, AuthenticatedUser));

            return new ImportResultDataContract();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to import setting groups");
            return new ImportResultDataContract
            {
                ErrorMessage = ex.Message
            };
        }
    }

    private static void ValidateImport(SettingGroupExportDataContract data, ImportType importType)
    {
        if (data?.Groups == null)
            throw new ArgumentException("Import data must contain a non-null groups collection.");

        if (importType is not (ImportType.ClearAndImport or ImportType.AddNew or ImportType.ReplaceExisting))
            throw new ArgumentOutOfRangeException(nameof(importType), importType, "Unsupported import type for group import.");

        foreach (var group in data.Groups)
        {
            if (string.IsNullOrWhiteSpace(group.Name))
                throw new ArgumentException("All imported groups must have a non-empty name.");
        }
    }

    private async Task ClearAndImport(SettingGroupExportDataContract data)
    {
        var existingGroups = await _settingGroupRepository.GetAllGroups();
        foreach (var existing in existingGroups)
        {
            await _settingGroupRepository.DeleteGroup(existing);
        }

        foreach (var group in data.Groups)
        {
            await CreateGroupEntity(group);
        }
    }

    private async Task AddNew(SettingGroupExportDataContract data)
    {
        foreach (var group in data.Groups)
        {
            var existing = await _settingGroupRepository.GetGroupByName(group.Name);
            if (existing == null)
            {
                await CreateGroupEntity(group);
            }
        }
    }

    private async Task ReplaceExisting(SettingGroupExportDataContract data)
    {
        foreach (var group in data.Groups)
        {
            var existing = await _settingGroupRepository.GetGroupByName(group.Name);
            if (existing != null)
            {
                existing.Description = group.Description;
                existing.GroupSettingsJson = SerializeGroupedSettings(group.GroupedSettings);
                existing.LastModifiedAt = DateTime.UtcNow;
                existing.LastModifiedBy = AuthenticatedUser?.Username;
                await _settingGroupRepository.UpdateGroup(existing);
            }
            else
            {
                await CreateGroupEntity(group);
            }
        }
    }

    private async Task CreateGroupEntity(SettingGroupDataContract group)
    {
        var entity = new SettingGroupBusinessEntity
        {
            Name = group.Name,
            Description = group.Description,
            GroupSettingsJson = SerializeGroupedSettings(group.GroupedSettings),
            CreatedAt = DateTime.UtcNow,
            LastModifiedAt = DateTime.UtcNow,
            LastModifiedBy = AuthenticatedUser?.Username ?? "Import"
        };
        await _settingGroupRepository.AddGroup(entity);
    }

    private static SettingGroupDataContract ConvertToDataContract(SettingGroupBusinessEntity entity)
    {
        var groupedSettings = DeserializeGroupedSettings(entity.GroupSettingsJson);
        return new SettingGroupDataContract(entity.Id, entity.Name, entity.Description, groupedSettings)
        {
            CreatedAt = entity.CreatedAt,
            LastModifiedAt = entity.LastModifiedAt,
            LastModifiedBy = entity.LastModifiedBy
        };
    }

    private static List<GroupedSettingDataContract> DeserializeGroupedSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<GroupedSettingDataContract>();

        return JsonConvert.DeserializeObject<List<GroupedSettingDataContract>>(json)
            ?? new List<GroupedSettingDataContract>();
    }

    private static string SerializeGroupedSettings(List<GroupedSettingDataContract> groupedSettings)
    {
        return JsonConvert.SerializeObject(groupedSettings);
    }
}
