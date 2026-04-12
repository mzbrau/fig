using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;

namespace Fig.Web.Facades;

public interface IGroupsFacade
{
    Task<List<SettingGroupDataContract>> GetAllGroups();

    Task<SettingGroupDataContract?> GetGroup(Guid id);

    Task<SettingGroupDataContract?> CreateGroup(SettingGroupDataContract group);

    Task<SettingGroupDataContract?> UpdateGroup(SettingGroupDataContract group);

    Task DeleteGroup(Guid id);

    Task<SettingGroupExportDataContract?> ExportGroups();

    Task<ImportResultDataContract?> ImportGroups(SettingGroupExportDataContract data, ImportType importType);
}
