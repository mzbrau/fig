using Fig.Common.Events;
using Fig.Contracts.ImportExport;
using Fig.Contracts.SettingGroups;
using Fig.Web.Events;
using Fig.Web.Services;

namespace Fig.Web.Facades;

public class GroupsFacade : IGroupsFacade
{
    private readonly IHttpService _httpService;

    public GroupsFacade(IHttpService httpService, IEventDistributor eventDistributor)
    {
        _httpService = httpService;
        eventDistributor.Subscribe(EventConstants.LogoutEvent, () =>
        {
            Items.Clear();
            ItemsChanged?.Invoke();
        });
    }

    public List<SettingGroupDataContract> Items { get; } = new();

    public event Action? ItemsChanged;

    public async Task LoadAll()
    {
        var groups = await _httpService.Get<List<SettingGroupDataContract>>("settinggroups");
        if (groups is null)
            return;

        var drafts = Items.Where(g => g.Id == null).ToList();
        Items.Clear();
        Items.AddRange(groups.OrderBy(g => g.Name));
        foreach (var draft in drafts)
        {
            if (Items.Any(g => string.Equals(g.Name, draft.Name, StringComparison.OrdinalIgnoreCase)))
                continue;
            Items.Add(draft);
        }

        ItemsChanged?.Invoke();
    }

    public async Task<List<SettingGroupDataContract>> GetAllGroups()
    {
        await LoadAll();
        return Items.ToList();
    }

    public async Task<SettingGroupDataContract?> GetGroup(Guid id)
    {
        return await _httpService.Get<SettingGroupDataContract>($"settinggroups/{id}");
    }

    public SettingGroupDataContract AddDraftGroup(
        string name,
        string? description = null,
        List<GroupedSettingDataContract>? groupedSettings = null)
    {
        var draft = new SettingGroupDataContract(
            null,
            name.Trim(),
            string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            groupedSettings ?? new List<GroupedSettingDataContract>());
        Items.Add(draft);
        ItemsChanged?.Invoke();
        return draft;
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

    public async Task<SettingGroupDataContract?> SaveGroup(SettingGroupDataContract group)
    {
        if (group.Id == null)
        {
            var created = await CreateGroup(group);
            if (created is null)
                return null;

            var draftIndex = Items.FindIndex(g =>
                g.Id == null &&
                string.Equals(g.Name, group.Name, StringComparison.OrdinalIgnoreCase));
            if (draftIndex >= 0)
                Items[draftIndex] = created;
            else
                Items.Add(created);

            ItemsChanged?.Invoke();
            return created;
        }

        var updated = await UpdateGroup(group);
        if (updated is null)
            return null;

        var existingIndex = Items.FindIndex(g => g.Id == updated.Id);
        if (existingIndex >= 0)
            Items[existingIndex] = updated;
        else
            Items.Add(updated);

        ItemsChanged?.Invoke();
        return updated;
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
