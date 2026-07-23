using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;

namespace Fig.Web.Facades;

public interface IGroupsFacade
{
    List<SettingGroupDataContract> Items { get; }

    event Action? ItemsChanged;

    Task LoadAll();

    Task<List<SettingGroupDataContract>> GetAllGroups();

    Task<SettingGroupDataContract?> GetGroup(Guid id);

    SettingGroupDataContract AddDraftGroup(
        string name,
        string? description = null,
        List<GroupedSettingDataContract>? groupedSettings = null);

    Task<SettingGroupDataContract?> CreateGroup(SettingGroupDataContract group);

    Task<SettingGroupDataContract?> UpdateGroup(SettingGroupDataContract group);

    Task<SettingGroupDataContract?> SaveGroup(SettingGroupDataContract group);

    Task DeleteGroup(Guid id);

    Task<SettingGroupExportDataContract?> ExportGroups();

    Task<ImportResultDataContract?> ImportGroups(SettingGroupExportDataContract data, ImportType importType);
}
