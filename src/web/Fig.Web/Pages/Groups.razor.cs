using Fig.Contracts.Authentication;
using Fig.Contracts.SettingGroups;
using Fig.Web.Facades;
using Fig.Web.Models.Setting;
using Fig.Web.Notifications;
using Fig.Web.Pages.Dialogs;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Newtonsoft.Json;
using Radzen;

namespace Fig.Web.Pages;

public partial class Groups : ComponentBase
{
    private const string TransparentColor = "#00000000";
    private const string SavePendingChangesResult = "save";
    private const string DiscardPendingChangesResult = "discard";

    [Inject] private IGroupsFacade GroupsFacade { get; set; } = null!;

    [Inject] private ISettingClientFacade SettingClientFacade { get; set; } = null!;

    [Inject] private IAccountService AccountService { get; set; } = null!;

    [Inject] private NavigationManager NavigationManager { get; set; } = null!;

    [Inject] private DialogService DialogService { get; set; } = null!;

    [Inject] private NotificationService NotificationService { get; set; } = null!;

    [Inject] private INotificationFactory NotificationFactory { get; set; } = null!;

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
    private readonly HashSet<string> _dirtyGroupKeys = new(StringComparer.Ordinal);
    private Dictionary<string, string> _baselineGroupSnapshots = new(StringComparer.Ordinal);
    private bool _allowNextNavigation;

    private bool IsAdmin => AccountService.AuthenticatedUser?.Role == Role.Administrator;
    private bool HasPendingChanges => _dirtyGroupKeys.Count > 0 || HasPendingEditorChanges;
    private bool HasPendingEditorChanges => HasPendingHeaderEdits() || HasPendingGroupedSettingEdits();
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

    private async Task LoadGroups(Guid? selectedGroupId = null)
    {
        _loading = true;
        StateHasChanged();

        try
        {
            var restoreSelectionId = selectedGroupId ?? _selectedGroup?.Id;
            _groups = await GroupsFacade.GetAllGroups();
            NormalizeGroups(_groups);
            _groups = _groups.OrderBy(g => g.Name).ToList();
            _selectedGroup = restoreSelectionId == null
                ? null
                : _groups.FirstOrDefault(g => g.Id == restoreSelectionId);
            ResetEditModes();
            CaptureBaseline();
        }
        finally
        {
            _loading = false;
            StateHasChanged();
        }
    }

    private void ResetEditModes()
    {
        _editingHeader = false;
        _editingGroupedSetting = null;
    }

    // ── Group CRUD ──

    private async Task AddGroup()
    {
        if (!await ResolvePendingChanges())
            return;

        var name = await DialogService.OpenAsync<TextPromptDialog>("New Group",
            new Dictionary<string, object?> { { "Prompt", "Enter group name:" } },
            new DialogOptions { Width = "400px" });

        if (name is string groupName && !string.IsNullOrWhiteSpace(groupName))
        {
            groupName = groupName.Trim();
            var newGroup = new SettingGroupDataContract(
                null, groupName, null, new List<GroupedSettingDataContract>());

            var created = await GroupsFacade.CreateGroup(newGroup);
            if (created != null)
            {
                NotificationService.Notify(NotificationFactory.Success("Group Created", $"Group '{groupName}' created successfully"));
                SettingClientFacade.MarkGroupsChanged();
                await LoadGroups(created.Id);
            }
        }
    }

    private async Task SaveGroup()
    {
        await SaveGroupInternal();
    }

    private async Task<bool> SaveGroupInternal()
    {
        if (_selectedGroup == null)
            return false;

        if (!TryApplyPendingEditorChanges())
            return false;

        NormalizeGroup(_selectedGroup);
        RefreshPendingChangeState();

        if (!HasPendingChanges)
            return true;

        var selectedGroupId = _selectedGroup.Id;
        var result = await GroupsFacade.UpdateGroup(_selectedGroup);
        if (result == null)
            return false;

        NotificationService.Notify(NotificationFactory.Success("Group Saved", $"Group '{_selectedGroup.Name}' saved successfully"));
        SettingClientFacade.MarkGroupsChanged();
        await LoadGroups(result.Id ?? selectedGroupId);
        return true;
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
            NotificationService.Notify(NotificationFactory.Success("Group Deleted", $"Group '{_selectedGroup.Name}' deleted"));
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
        if (!TryApplyPendingHeaderEdit())
            return;

        RefreshPendingChangeState();
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
        ApplyPendingGroupedSettingEdit();
        RefreshPendingChangeState();
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
            NotificationService.Notify(NotificationFactory.Info("No Available Settings", "All settings are already assigned to groups."));
            return;
        }

        var settingValueTypes = BuildSettingValueTypeLookup();

        var selected = await DialogService.OpenAsync<SourceSettingSelectionDialog>(
            "Select Source Settings for New Grouped Setting",
            new Dictionary<string, object?>
            {
                { "AvailableSettings", availableSettings },
                { "ValueTypeFilter", null },
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
        RefreshPendingChangeState();
        StateHasChanged();
    }

    private void RemoveGroupedSetting(GroupedSettingDataContract groupedSetting)
    {
        _selectedGroup?.GroupedSettings.Remove(groupedSetting);
        if (_editingGroupedSetting == groupedSetting)
            _editingGroupedSetting = null;
        RefreshPendingChangeState();
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
            NotificationService.Notify(NotificationFactory.Info("No Available Settings", "All settings are already assigned to groups."));
            return;
        }

        var settingValueTypes = BuildSettingValueTypeLookup();

        var selected = await DialogService.OpenAsync<SourceSettingSelectionDialog>(
            "Select Source Settings",
            new Dictionary<string, object?>
            {
                { "AvailableSettings", availableSettings },
                { "ValueTypeFilter", groupedSetting.SourceSettings.Any() ? groupedSetting.ValueType : null },
                { "SettingValueTypes", settingValueTypes }
            },
            new DialogOptions { Width = "600px", Height = "500px" });

        if (selected is not List<SourceSettingDataContract> selectedSettings || !selectedSettings.Any())
            return;

        if (ReferenceEquals(_editingGroupedSetting, groupedSetting))
            ApplyPendingGroupedSettingEdit();

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
        RefreshPendingChangeState();
        StateHasChanged();
    }

    private void RemoveSourceSetting(GroupedSettingDataContract groupedSetting, SourceSettingDataContract sourceSetting)
    {
        if (groupedSetting.SourceSettings.Count <= 1)
        {
            NotificationService.Notify(NotificationFactory.Warning(
                "Cannot Remove",
                "A grouped setting must have at least one source setting. Remove the grouped setting instead."));
            return;
        }

        if (ReferenceEquals(_editingGroupedSetting, groupedSetting))
            ApplyPendingGroupedSettingEdit();

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

        RefreshPendingChangeState();
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
        if (index < 0 || index >= groupedSetting.SourceSettings.Count ||
            targetIndex < 0 || targetIndex >= groupedSetting.SourceSettings.Count)
            return;

        if (ReferenceEquals(_editingGroupedSetting, groupedSetting))
            ApplyPendingGroupedSettingEdit();

        var previousPrimaryName = GetSourceDerivedName(groupedSetting.SourceSettings.FirstOrDefault());
        MoveItem(groupedSetting.SourceSettings, index, targetIndex);
        ApplyGroupedSettingDerivedMetadata(groupedSetting, previousPrimaryName);
        RefreshPendingChangeState();
        StateHasChanged();
    }

    private async Task OnGroupSelectionChanged(SettingGroupDataContract? targetGroup)
    {
        if (targetGroup == null || targetGroup.Id == _selectedGroup?.Id)
            return;

        if (!await ResolvePendingChanges())
        {
            await InvokeAsync(StateHasChanged);
            return;
        }

        _selectedGroup = _groups.FirstOrDefault(group => group.Id == targetGroup.Id);
        ResetEditModes();
        await InvokeAsync(StateHasChanged);
    }

    private async Task OnBeforeInternalNavigation(LocationChangingContext context)
    {
        if (_allowNextNavigation)
        {
            _allowNextNavigation = false;
            return;
        }

        if (!HasPendingChanges || !IsLeavingGroupsPage(context.TargetLocation))
            return;

        context.PreventNavigation();

        if (!await ResolvePendingChanges())
            return;

        _allowNextNavigation = true;
        NavigationManager.NavigateTo(context.TargetLocation);
    }

    // ── Helpers ──

    private static int GetTotalSourceSettingCount(SettingGroupDataContract group)
    {
        return group.GroupedSettings.Sum(groupedSetting => groupedSetting.SourceSettings.Count);
    }

    private bool IsGroupDirty(SettingGroupDataContract group)
    {
        var isSelectedGroupWithPendingEditorChanges =
            _selectedGroup != null &&
            group.Id == _selectedGroup.Id &&
            HasPendingEditorChanges;

        return _dirtyGroupKeys.Contains(GetGroupKey(group)) || isSelectedGroupWithPendingEditorChanges;
    }

    private string GetPendingChangeSummary()
    {
        var pendingGroupCount = GetPendingGroupCount();
        return pendingGroupCount == 1
            ? "1 group has unsaved changes"
            : $"{pendingGroupCount} groups have unsaved changes";
    }

    private string GetSaveButtonText()
    {
        var pendingGroupCount = GetPendingGroupCount();
        return pendingGroupCount > 0 ? $"Save ({pendingGroupCount})" : "Save";
    }

    private int GetPendingGroupCount()
    {
        var pendingGroupCount = _dirtyGroupKeys.Count;
        if (_selectedGroup != null &&
            HasPendingEditorChanges &&
            !_dirtyGroupKeys.Contains(GetGroupKey(_selectedGroup)))
        {
            pendingGroupCount++;
        }

        return pendingGroupCount;
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

    private async Task<bool> ResolvePendingChanges()
    {
        if (!HasPendingChanges)
            return true;

        var result = await DialogService.OpenAsync<UnsavedChangesDialog>(
            "Unsaved Changes",
            new Dictionary<string, object?>
            {
                { "Message", "You have unsaved changes on this page. Save before leaving?" }
            },
            new DialogOptions { Width = "460px", CloseDialogOnOverlayClick = false, CloseDialogOnEsc = true });

        if (result is not string action)
            return false;

        switch (action)
        {
            case SavePendingChangesResult:
                return await SaveGroupInternal();
            case DiscardPendingChangesResult:
                await LoadGroups();
                return true;
            default:
                return false;
        }
    }

    private bool TryApplyPendingEditorChanges()
    {
        if (!TryApplyPendingHeaderEdit())
            return false;

        ApplyPendingGroupedSettingEdit();
        return true;
    }

    private bool TryApplyPendingHeaderEdit()
    {
        if (!_editingHeader || _selectedGroup == null)
            return true;

        var normalizedName = _editName.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            NotificationService.Notify(NotificationFactory.Warning("Group Name Required", "Enter a group name before saving."));
            return false;
        }

        _selectedGroup.Name = normalizedName;
        _selectedGroup.Description = NormalizeOptionalText(_editDescription);
        _editingHeader = false;
        return true;
    }

    private void ApplyPendingGroupedSettingEdit()
    {
        if (_editingGroupedSetting == null)
            return;

        var primarySourceName = GetSourceDerivedName(_editingGroupedSetting.SourceSettings.FirstOrDefault());
        _editingGroupedSetting.Name = string.IsNullOrWhiteSpace(_gsEditName)
            ? primarySourceName
            : _gsEditName.Trim();
        _editingGroupedSetting.Description = GetEditedGroupedSettingDescription(_editingGroupedSetting);
        ApplyGroupedSettingDerivedMetadata(_editingGroupedSetting);
        _editingGroupedSetting = null;
    }

    private bool HasPendingHeaderEdits()
    {
        if (!_editingHeader || _selectedGroup == null)
            return false;

        var normalizedName = _editName.Trim();
        var normalizedDescription = NormalizeOptionalText(_editDescription);
        return !string.Equals(_selectedGroup.Name, normalizedName, StringComparison.Ordinal) ||
               !string.Equals(_selectedGroup.Description, normalizedDescription, StringComparison.Ordinal);
    }

    private bool HasPendingGroupedSettingEdits()
    {
        if (_editingGroupedSetting == null)
            return false;

        var primarySourceName = GetSourceDerivedName(_editingGroupedSetting.SourceSettings.FirstOrDefault());
        var updatedName = string.IsNullOrWhiteSpace(_gsEditName)
            ? primarySourceName
            : _gsEditName.Trim();
        var updatedDescription = GetEditedGroupedSettingDescription(_editingGroupedSetting);

        return !string.Equals(_editingGroupedSetting.Name, updatedName, StringComparison.Ordinal) ||
               !string.Equals(_editingGroupedSetting.Description, updatedDescription, StringComparison.Ordinal);
    }

    private void CaptureBaseline()
    {
        _baselineGroupSnapshots = CreateGroupSnapshots(_groups);
        _dirtyGroupKeys.Clear();
    }

    private void RefreshPendingChangeState()
    {
        var currentSnapshots = CreateGroupSnapshots(_groups);
        _dirtyGroupKeys.Clear();

        foreach (var group in _groups)
        {
            var key = GetGroupKey(group);
            if (currentSnapshots.TryGetValue(key, out var currentSnapshot) &&
                (!_baselineGroupSnapshots.TryGetValue(key, out var baselineSnapshot) ||
                 !string.Equals(currentSnapshot, baselineSnapshot, StringComparison.Ordinal)))
            {
                _dirtyGroupKeys.Add(key);
            }
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

    private Dictionary<string, string> CreateGroupSnapshots(IEnumerable<SettingGroupDataContract> groups)
    {
        var clonedGroups = JsonConvert.DeserializeObject<List<SettingGroupDataContract>>(
                               JsonConvert.SerializeObject(groups))
                           ?? new List<SettingGroupDataContract>();

        NormalizeGroups(clonedGroups);
        return clonedGroups.ToDictionary(GetGroupKey, JsonConvert.SerializeObject, StringComparer.Ordinal);
    }

    private static string GetGroupKey(SettingGroupDataContract group)
    {
        return group.Id?.ToString() ?? group.Name;
    }

    private static string? NormalizeOptionalText(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private string? GetEditedGroupedSettingDescription(GroupedSettingDataContract groupedSetting)
    {
        var trimmed = _gsEditDescription.Trim();
        if (!string.IsNullOrWhiteSpace(trimmed))
            return trimmed;

        return groupedSetting.Description == null ? null : string.Empty;
    }

    private bool IsLeavingGroupsPage(string targetLocation)
    {
        var relativePath = NavigationManager.ToBaseRelativePath(targetLocation);
        var path = relativePath.Split('?', '#')[0].Trim('/');
        return !string.Equals(path, "groups", StringComparison.OrdinalIgnoreCase);
    }
}
