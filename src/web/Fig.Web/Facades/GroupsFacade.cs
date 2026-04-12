using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class GroupsFacade : IGroupsFacade
{
    private readonly IHttpService _httpService;

    public GroupsFacade(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<List<SettingGroupDataContract>> GetAllGroups()
    {
        return await _httpService.Get<List<SettingGroupDataContract>>("settinggroups")
               ?? new List<SettingGroupDataContract>();
    }

    public async Task<SettingGroupDataContract?> GetGroup(Guid id)
    {
        return await _httpService.Get<SettingGroupDataContract>($"settinggroups/{id}");
    }

    public async Task<SettingGroupDataContract?> CreateGroup(SettingGroupDataContract group)
    {
        return await _httpService.Post<SettingGroupDataContract>("settinggroups", group);
    }

    public async Task<SettingGroupDataContract?> UpdateGroup(SettingGroupDataContract group)
    {
        if (group.Id is not { } id)
            throw new ArgumentException("Group id is required when updating a group.", nameof(group));

        return await _httpService.Put<SettingGroupDataContract>($"settinggroups/{id}", group);
    }

    public async Task DeleteGroup(Guid id)
    {
        await _httpService.Delete($"settinggroups/{id}");
    }

    public async Task<SettingGroupExportDataContract?> ExportGroups()
    {
        return await _httpService.Get<SettingGroupExportDataContract>("settinggroupdata");
    }

    public async Task<ImportResultDataContract?> ImportGroups(SettingGroupExportDataContract data, ImportType importType)
    {
        return await _httpService.Put<ImportResultDataContract>($"settinggroupdata?importType={importType}", data);
    }
}
