using Fig.Contracts.Authentication;
using Fig.Contracts.SettingGroups;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Pages.Dialogs;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages;

public partial class Groups : ComponentBase
{
    private const string TransparentColor = "#00000000";

    [Inject] private IGroupsFacade GroupsFacade { get; set; } = null!;

    [Inject] private ISettingClientFacade SettingClientFacade { get; set; } = null!;

    [Inject] private IAccountService AccountService { get; set; } = null!;

    [Inject] private DialogService DialogService { get; set; } = null!;

    [Inject] private NotificationService NotificationService { get; set; } = null!;

    private List<SettingGroupDataContract> _groups = new();
    private SettingGroupDataContract? _selectedGroup;
    private bool _loading = true;
    private string _groupFilterText = string.Empty;

    // Header edit state
    private bool _editingHeader;
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;

    // Grouped setting edit state — tracked by reference, not index
    private GroupedSettingDataContract? _editingGroupedSetting;
    private string _gsEditName = string.Empty;
    private string _gsEditDescription = string.Empty;

    private bool IsAdmin => AccountService.AuthenticatedUser?.Role == Role.Administrator;
    private IEnumerable<SettingGroupDataContract> FilteredGroups => string.IsNullOrWhiteSpace(_groupFilterText)
        ? _groups
        : _groups.Where(group =>
            group.Name.Contains(_groupFilterText, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
        // Ensure settings clients are loaded so source setting selection works
        // even when user navigates directly to /groups
        await SettingClientFacade.LoadAllClients();
        await LoadGroups();
        await base.OnInitializedAsync();
    }

    private async Task LoadGroups()
    {
        _loading = true;
        StateHasChanged();

        try
        {
            _groups = await GroupsFacade.GetAllGroups();
            NormalizeGroups(_groups);
            _groups = _groups.OrderBy(g => g.Name).ToList();

            if (_selectedGroup != null)
            {
                _selectedGroup = _groups.FirstOrDefault(g => g.Id == _selectedGroup.Id);
            }
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private void OnGroupSelected()
    {
        _editingHeader = false;
        _editingGroupedSetting = null;
    }

    // ── Group CRUD ──

    private async Task AddGroup()
    {
        var name = await DialogService.OpenAsync<TextPromptDialog>("New Group",
            new Dictionary<string, object> { { "Prompt", "Enter group name:" } },
            new DialogOptions { Width = "400px" });

        if (name is string groupName && !string.IsNullOrWhiteSpace(groupName))
        {
            var newGroup = new SettingGroupDataContract(
                null, groupName, null, new List<GroupedSettingDataContract>());

            var created = await GroupsFacade.CreateGroup(newGroup);
            if (created != null)
            {
                NotificationService.Notify(new NotificationMessage
                {
                    Severity = NotificationSeverity.Success,
                    Summary = "Group Created",
                    Detail = $"Group '{groupName}' created successfully"
                });
                SettingClientFacade.MarkGroupsChanged();
                await LoadGroups();
                _selectedGroup = _groups.FirstOrDefault(g => g.Id == created.Id);
            }
        }
    }

    private async Task SaveGroup()
    {
        if (_selectedGroup == null)
            return;

        NormalizeGroup(_selectedGroup);

        var result = await GroupsFacade.UpdateGroup(_selectedGroup);
        if (result != null)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Group Saved",
                Detail = $"Group '{_selectedGroup.Name}' saved successfully"
            });
            SettingClientFacade.MarkGroupsChanged();
            await LoadGroups();
        }
    }

    private async Task DeleteGroup()
    {
        if (_selectedGroup?.Id == null)
            return;

        var confirm = await DialogService.Confirm(
            $"Are you sure you want to delete the group '{_selectedGroup.Name}'? Settings will retain their values but will no longer be grouped.",
            "Delete Group",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" });

        if (confirm == true)
        {
            await GroupsFacade.DeleteGroup(_selectedGroup.Id.Value);
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Success,
                Summary = "Group Deleted",
                Detail = $"Group '{_selectedGroup.Name}' deleted"
            });
            SettingClientFacade.MarkGroupsChanged();
            _selectedGroup = null;
            await LoadGroups();
        }
    }

    // ── Header edit mode ──

    private void StartHeaderEdit()
    {
        _editName = _selectedGroup?.Name ?? string.Empty;
        _editDescription = _selectedGroup?.Description ?? string.Empty;
        _editingHeader = true;
    }

    private void SaveHeaderEdit()
    {
        if (_selectedGroup == null)
            return;

        _selectedGroup.Name = _editName;
        _selectedGroup.Description = _editDescription;
        _editingHeader = false;
    }

    private void CancelHeaderEdit()
    {
        _editingHeader = false;
    }

    // ── Grouped setting edit mode ──

    private void StartSettingEdit(GroupedSettingDataContract groupedSetting)
    {
        _editingGroupedSetting = groupedSetting;
        _gsEditName = groupedSetting.Name;
        _gsEditDescription = groupedSetting.Description ?? string.Empty;
    }

    private void SaveSettingEdit()
    {
        if (_editingGroupedSetting == null)
            return;

        var primarySourceName = GetSourceDerivedName(_editingGroupedSetting.SourceSettings.FirstOrDefault());
        _editingGroupedSetting.Name = string.IsNullOrWhiteSpace(_gsEditName)
            ? primarySourceName
            : _gsEditName.Trim();
        _editingGroupedSetting.Description = string.IsNullOrWhiteSpace(_gsEditDescription)
            ? null
            : _gsEditDescription.Trim();
        ApplyGroupedSettingDerivedMetadata(_editingGroupedSetting);
        _editingGroupedSetting = null;
    }

    private void CancelSettingEdit()
    {
        _editingGroupedSetting = null;
    }

    // ── Grouped setting management ──

    private async Task AddGroupedSetting()
    {
        if (_selectedGroup == null)
            return;

        var allGroupedSourceSettings = _groups
            .SelectMany(g => g.GroupedSettings)
            .SelectMany(setting => setting.SourceSettings)
            .Select(setting => $"{setting.ClientName}|{setting.SettingName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var availableSettings = SettingClientFacade.SettingClients
            .Where(client => !client.IsGroup && client.Instance == null)
            .SelectMany(client => client.Settings.Select(setting => new SourceSettingDataContract(client.Name, setting.Name)))
            .Where(setting => !allGroupedSourceSettings.Contains($"{setting.ClientName}|{setting.SettingName}"))
            .OrderBy(setting => setting.ClientName)
            .ThenBy(setting => setting.SettingName)
            .ToList();

        if (!availableSettings.Any())
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "No Available Settings",
                Detail = "All settings are already assigned to groups."
            });
            return;
        }

        var settingValueTypes = BuildSettingValueTypeLookup();

        var selected = await DialogService.OpenAsync<SourceSettingSelectionDialog>(
            "Select Source Settings for New Grouped Setting",
            new Dictionary<string, object>
            {
                { "AvailableSettings", availableSettings },
                { "ValueTypeFilter", null! },
                { "SettingValueTypes", settingValueTypes }
            },
            new DialogOptions { Width = "600px", Height = "500px" });

        if (selected is not List<SourceSettingDataContract> selectedSettings || !selectedSettings.Any())
            return;

        var firstSource = selectedSettings.First();
        var templateSetting = FindSetting(firstSource.ClientName, firstSource.SettingName);
        var valueType = settingValueTypes.TryGetValue($"{firstSource.ClientName}|{firstSource.SettingName}", out var discoveredValueType)
            ? discoveredValueType
            : "System.String";

        var groupedSetting = new GroupedSettingDataContract(
            templateSetting?.Name ?? firstSource.SettingName,
            templateSetting?.RawDescription,
            valueType,
            selectedSettings);

        ApplyGroupedSettingDerivedMetadata(groupedSetting, forceNameFromPrimarySource: true);
        _selectedGroup.GroupedSettings.Add(groupedSetting);
        StateHasChanged();
    }

    private void RemoveGroupedSetting(GroupedSettingDataContract groupedSetting)
    {
        _selectedGroup?.GroupedSettings.Remove(groupedSetting);
        if (_editingGroupedSetting == groupedSetting)
            _editingGroupedSetting = null;
        StateHasChanged();
    }

    private async Task AddSourceSetting(GroupedSettingDataContract groupedSetting)
    {
        if (_selectedGroup == null)
            return;

        var allGroupedSourceSettings = _groups
            .SelectMany(g => g.GroupedSettings)
            .SelectMany(setting => setting.SourceSettings)
            .Select(setting => $"{setting.ClientName}|{setting.SettingName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var availableSettings = SettingClientFacade.SettingClients
            .Where(client => !client.IsGroup && client.Instance == null)
            .SelectMany(client => client.Settings.Select(setting => new SourceSettingDataContract(client.Name, setting.Name)))
            .Where(setting => !allGroupedSourceSettings.Contains($"{setting.ClientName}|{setting.SettingName}"))
            .OrderBy(setting => setting.ClientName)
            .ThenBy(setting => setting.SettingName)
            .ToList();

        if (!availableSettings.Any())
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Info,
                Summary = "No Available Settings",
                Detail = "All settings are already assigned to groups."
            });
            return;
        }

        var settingValueTypes = BuildSettingValueTypeLookup();

        var selected = await DialogService.OpenAsync<SourceSettingSelectionDialog>(
            "Select Source Settings",
            new Dictionary<string, object>
            {
                { "AvailableSettings", availableSettings },
                { "ValueTypeFilter", groupedSetting.SourceSettings.Any() ? groupedSetting.ValueType : null! },
                { "SettingValueTypes", settingValueTypes }
            },
            new DialogOptions { Width = "600px", Height = "500px" });

        if (selected is not List<SourceSettingDataContract> selectedSettings || !selectedSettings.Any())
            return;

        var previousPrimaryName = GetSourceDerivedName(groupedSetting.SourceSettings.FirstOrDefault());

        foreach (var setting in selectedSettings)
        {
            if (!groupedSetting.SourceSettings.Any(existing =>
                    string.Equals(existing.ClientName, setting.ClientName, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(existing.SettingName, setting.SettingName, StringComparison.Ordinal)))
            {
                groupedSetting.SourceSettings.Add(setting);
            }
        }

        ApplyGroupedSettingDerivedMetadata(groupedSetting, previousPrimaryName);
        StateHasChanged();
    }

    private void RemoveSourceSetting(GroupedSettingDataContract groupedSetting, SourceSettingDataContract sourceSetting)
    {
        if (groupedSetting.SourceSettings.Count <= 1)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Cannot Remove",
                Detail = "A grouped setting must have at least one source setting. Remove the grouped setting instead."
            });
            return;
        }

        var previousPrimaryName = GetSourceDerivedName(groupedSetting.SourceSettings.FirstOrDefault());
        var removedPrimarySource = MatchesSourceSetting(groupedSetting.SourceSettings.First(), sourceSetting);

        groupedSetting.SourceSettings.Remove(sourceSetting);

        if (removedPrimarySource)
        {
            ApplyGroupedSettingDerivedMetadata(groupedSetting, previousPrimaryName);
        }
        else
        {
            ApplyGroupedSettingDerivedMetadata(groupedSetting);
        }

        StateHasChanged();
    }

    private void MoveSourceSettingUp(GroupedSettingDataContract groupedSetting, int index)
    {
        MoveSourceSetting(groupedSetting, index, -1);
    }

    private void MoveSourceSettingDown(GroupedSettingDataContract groupedSetting, int index)
    {
        MoveSourceSetting(groupedSetting, index, 1);
    }

    private void MoveSourceSetting(GroupedSettingDataContract groupedSetting, int index, int offset)
    {
        var targetIndex = index + offset;
        if (targetIndex < 0 || targetIndex >= groupedSetting.SourceSettings.Count)
            return;

        var previousPrimaryName = GetSourceDerivedName(groupedSetting.SourceSettings.FirstOrDefault());
        MoveItem(groupedSetting.SourceSettings, index, targetIndex);
        ApplyGroupedSettingDerivedMetadata(groupedSetting, previousPrimaryName);
        StateHasChanged();
    }

    // ── Helpers ──

    private static int GetTotalSourceSettingCount(SettingGroupDataContract group)
    {
        return group.GroupedSettings.Sum(groupedSetting => groupedSetting.SourceSettings.Count);
    }

    private void NormalizeGroups(IEnumerable<SettingGroupDataContract> groups)
    {
        foreach (var group in groups)
        {
            NormalizeGroup(group);
        }
    }

    private void NormalizeGroup(SettingGroupDataContract group)
    {
        foreach (var groupedSetting in group.GroupedSettings)
        {
            ApplyGroupedSettingDerivedMetadata(groupedSetting);
        }
    }

    private void ApplyGroupedSettingDerivedMetadata(
        GroupedSettingDataContract groupedSetting,
        string? previousDerivedName = null,
        bool forceNameFromPrimarySource = false)
    {
        if (!groupedSetting.SourceSettings.Any())
            return;

        var firstSource = groupedSetting.SourceSettings.First();
        var templateSetting = FindSetting(firstSource.ClientName, firstSource.SettingName);
        var derivedName = templateSetting?.Name ?? firstSource.SettingName;

        if (forceNameFromPrimarySource ||
            string.IsNullOrWhiteSpace(groupedSetting.Name) ||
            (!string.IsNullOrWhiteSpace(previousDerivedName) &&
             string.Equals(groupedSetting.Name, previousDerivedName, StringComparison.Ordinal)))
        {
            groupedSetting.Name = derivedName;
        }

        if (forceNameFromPrimarySource &&
            string.IsNullOrWhiteSpace(groupedSetting.Description) &&
            !string.IsNullOrWhiteSpace(templateSetting?.RawDescription))
        {
            groupedSetting.Description = templateSetting.RawDescription;
        }

        groupedSetting.CategoryName = string.IsNullOrWhiteSpace(templateSetting?.CategoryName)
            ? null
            : templateSetting.CategoryName;
        groupedSetting.CategoryColor = NormalizeDerivedColor(templateSetting?.CategoryColor);

        SyncSettingEditState(groupedSetting);
    }

    private string GetSourceDerivedName(SourceSettingDataContract? sourceSetting)
    {
        if (sourceSetting == null)
            return string.Empty;

        return FindSetting(sourceSetting.ClientName, sourceSetting.SettingName)?.Name ?? sourceSetting.SettingName;
    }

    private void SyncSettingEditState(GroupedSettingDataContract groupedSetting)
    {
        if (!ReferenceEquals(_editingGroupedSetting, groupedSetting))
            return;

        _gsEditName = groupedSetting.Name;
        _gsEditDescription = groupedSetting.Description ?? string.Empty;
    }

    private static string? NormalizeDerivedColor(string? color)
    {
        return string.IsNullOrWhiteSpace(color) || color == TransparentColor
            ? null
            : color;
    }

    private static bool MatchesSourceSetting(SourceSettingDataContract left, SourceSettingDataContract right)
    {
        return string.Equals(left.ClientName, right.ClientName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(left.SettingName, right.SettingName, StringComparison.Ordinal);
    }

    private static void MoveItem<T>(IList<T> items, int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
            return;

        var item = items[fromIndex];
        items.RemoveAt(fromIndex);
        items.Insert(toIndex, item);
    }

    private ISetting? FindSetting(string clientName, string settingName)
    {
        var client = SettingClientFacade.SettingClients
            .FirstOrDefault(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase)
                                 && c.Instance == null && !c.IsGroup);
        return client?.Settings.FirstOrDefault(setting =>
            string.Equals(setting.Name, settingName, StringComparison.Ordinal));
    }

    private Dictionary<string, string> BuildSettingValueTypeLookup()
    {
        return SettingClientFacade.SettingClients
            .Where(client => !client.IsGroup && client.Instance == null)
            .SelectMany(client => client.Settings.Select(setting => new
            {
                Key = $"{client.Name}|{setting.Name}",
                ValueType = setting.ValueType?.FullName ?? "System.String"
            }))
            .GroupBy(item => item.Key)
            .ToDictionary(grouping => grouping.Key, grouping => grouping.First().ValueType, StringComparer.OrdinalIgnoreCase);
    }
}
