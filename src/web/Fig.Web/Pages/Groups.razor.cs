using Fig.Contracts.Authentication;
using Fig.Contracts.SettingGroups;
using Fig.Web.Facades;
using Fig.Web.Pages.Dialogs;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages;

public partial class Groups : ComponentBase
{
    [Inject] private IGroupsFacade GroupsFacade { get; set; } = null!;

    [Inject] private ISettingClientFacade SettingClientFacade { get; set; } = null!;

    [Inject] private IAccountService AccountService { get; set; } = null!;

    [Inject] private DialogService DialogService { get; set; } = null!;

    [Inject] private NotificationService NotificationService { get; set; } = null!;

    [Inject] private TooltipService TooltipService { get; set; } = null!;

    private List<SettingGroupDataContract> _groups = new();
    private SettingGroupDataContract? _selectedGroup;
    private bool _loading = true;

    // Header edit state
    private bool _editingHeader;
    private string _editName = string.Empty;
    private string _editDescription = string.Empty;

    // Grouped setting edit state — tracked by reference, not index
    private GroupedSettingDataContract? _editingGroupedSetting;
    private string _gsEditName = string.Empty;
    private string _gsEditDescription = string.Empty;
    private string _gsEditCategoryName = string.Empty;
    private string _gsEditCategoryColor = string.Empty;

    private bool IsAdmin => AccountService.AuthenticatedUser?.Role == Role.Administrator;

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
        // Reset edit states when selecting a different group
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
        if (_selectedGroup == null) return;

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
        if (_selectedGroup?.Id == null) return;

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
        if (_selectedGroup == null) return;
        _selectedGroup.Name = _editName;
        _selectedGroup.Description = _editDescription;
        _editingHeader = false;
    }

    private void CancelHeaderEdit()
    {
        _editingHeader = false;
    }

    // ── Grouped setting edit mode ──

    private void StartSettingEdit(GroupedSettingDataContract gs)
    {
        _editingGroupedSetting = gs;
        _gsEditName = gs.Name;
        _gsEditDescription = gs.Description ?? string.Empty;
        _gsEditCategoryName = gs.CategoryName ?? string.Empty;
        _gsEditCategoryColor = gs.CategoryColor ?? string.Empty;
    }

    private void SaveSettingEdit()
    {
        if (_editingGroupedSetting == null) return;

        _editingGroupedSetting.Name = _gsEditName;
        _editingGroupedSetting.Description = string.IsNullOrWhiteSpace(_gsEditDescription) ? null : _gsEditDescription;
        _editingGroupedSetting.CategoryName = string.IsNullOrWhiteSpace(_gsEditCategoryName) ? null : _gsEditCategoryName;
        _editingGroupedSetting.CategoryColor = string.IsNullOrWhiteSpace(_gsEditCategoryColor) ? null : _gsEditCategoryColor;
        _editingGroupedSetting = null;
    }

    private void CancelSettingEdit()
    {
        _editingGroupedSetting = null;
    }

    // ── Grouped setting management ──

    private async Task AddGroupedSetting()
    {
        if (_selectedGroup == null) return;

        // Step 1: open source setting selection first
        var allGroupedSourceSettings = _groups
            .SelectMany(g => g.GroupedSettings)
            .SelectMany(s => s.SourceSettings)
            .Select(s => $"{s.ClientName}|{s.SettingName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var availableSettings = SettingClientFacade.SettingClients
            .Where(c => !c.IsGroup && c.Instance == null)
            .SelectMany(c => c.Settings.Select(s => new SourceSettingDataContract(c.Name, s.Name)))
            .Where(s => !allGroupedSourceSettings.Contains($"{s.ClientName}|{s.SettingName}"))
            .OrderBy(s => s.ClientName)
            .ThenBy(s => s.SettingName)
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

        // Step 2: auto-populate from first source setting
        var firstSource = selectedSettings.First();
        var templateSetting = FindSetting(firstSource.ClientName, firstSource.SettingName);

        var valueType = settingValueTypes.TryGetValue($"{firstSource.ClientName}|{firstSource.SettingName}", out var vt)
            ? vt : "System.String";

        var newGs = new GroupedSettingDataContract(
            templateSetting?.Name ?? firstSource.SettingName,
            templateSetting?.RawDescription,
            valueType,
            selectedSettings)
        {
            CategoryName = templateSetting?.CategoryName,
            CategoryColor = templateSetting?.CategoryColor
        };

        _selectedGroup.GroupedSettings.Add(newGs);
        StateHasChanged();
    }

    private void RemoveGroupedSetting(GroupedSettingDataContract gs)
    {
        _selectedGroup?.GroupedSettings.Remove(gs);
        if (_editingGroupedSetting == gs)
            _editingGroupedSetting = null;
        StateHasChanged();
    }

    private async Task AddSourceSetting(GroupedSettingDataContract gs)
    {
        if (_selectedGroup == null) return;

        var allGroupedSourceSettings = _groups
            .SelectMany(g => g.GroupedSettings)
            .SelectMany(s => s.SourceSettings)
            .Select(s => $"{s.ClientName}|{s.SettingName}")
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var availableSettings = SettingClientFacade.SettingClients
            .Where(c => !c.IsGroup && c.Instance == null)
            .SelectMany(c => c.Settings.Select(s => new SourceSettingDataContract(c.Name, s.Name)))
            .Where(s => !allGroupedSourceSettings.Contains($"{s.ClientName}|{s.SettingName}"))
            .OrderBy(s => s.ClientName)
            .ThenBy(s => s.SettingName)
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
                { "ValueTypeFilter", gs.SourceSettings.Any() ? gs.ValueType : null! },
                { "SettingValueTypes", settingValueTypes }
            },
            new DialogOptions { Width = "600px", Height = "500px" });

        if (selected is List<SourceSettingDataContract> selectedSettings)
        {
            foreach (var setting in selectedSettings)
            {
                if (!gs.SourceSettings.Any(s =>
                        string.Equals(s.ClientName, setting.ClientName, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(s.SettingName, setting.SettingName, StringComparison.Ordinal)))
                {
                    gs.SourceSettings.Add(setting);
                }
            }

            // Auto-populate name/description/category from first source if this is the first time
            if (gs.SourceSettings.Count == selectedSettings.Count)
            {
                var first = gs.SourceSettings.First();
                var tmpl = FindSetting(first.ClientName, first.SettingName);
                if (tmpl != null)
                {
                    gs.Name = tmpl.Name;
                    gs.Description = tmpl.RawDescription;
                    if (settingValueTypes.TryGetValue($"{first.ClientName}|{first.SettingName}", out var fvt))
                        gs.ValueType = fvt;
                    gs.CategoryName ??= tmpl.CategoryName;
                    gs.CategoryColor ??= tmpl.CategoryColor;
                }
            }

            StateHasChanged();
        }
    }

    private void RemoveSourceSetting(GroupedSettingDataContract gs, SourceSettingDataContract ss)
    {
        if (gs.SourceSettings.Count <= 1)
        {
            NotificationService.Notify(new NotificationMessage
            {
                Severity = NotificationSeverity.Warning,
                Summary = "Cannot Remove",
                Detail = "A grouped setting must have at least one source setting. Remove the grouped setting instead."
            });
            return;
        }

        gs.SourceSettings.Remove(ss);
        StateHasChanged();
    }

    // ── Helpers ──

    private Models.Setting.ISetting? FindSetting(string clientName, string settingName)
    {
        var client = SettingClientFacade.SettingClients
            .FirstOrDefault(c => string.Equals(c.Name, clientName, StringComparison.OrdinalIgnoreCase)
                                 && c.Instance == null && !c.IsGroup);
        return client?.Settings.FirstOrDefault(s =>
            string.Equals(s.Name, settingName, StringComparison.Ordinal));
    }

    private Dictionary<string, string> BuildSettingValueTypeLookup()
    {
        return SettingClientFacade.SettingClients
            .Where(c => !c.IsGroup && c.Instance == null)
            .SelectMany(c => c.Settings.Select(s => new
            {
                Key = $"{c.Name}|{s.Name}",
                ValueType = s.ValueType?.FullName ?? "System.String"
            }))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().ValueType, StringComparer.OrdinalIgnoreCase);
    }

    private void ShowTooltip(ElementReference element, string text)
    {
        TooltipService.Open(element, text, new TooltipOptions
        {
            Position = TooltipPosition.Top,
            Duration = 3000
        });
    }
}
