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

    private List<SettingGroupDataContract> _groups = new();
    private SettingGroupDataContract? _selectedGroup;
    private string _filterText = string.Empty;
    private bool _loading = true;

    private bool IsAdmin => AccountService.AuthenticatedUser?.Role == Role.Administrator;

    private IEnumerable<SettingGroupDataContract> FilteredGroups =>
        string.IsNullOrWhiteSpace(_filterText)
            ? _groups
            : _groups.Where(g => g.Name.Contains(_filterText, StringComparison.OrdinalIgnoreCase));

    protected override async Task OnInitializedAsync()
    {
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

    private void SelectGroup(SettingGroupDataContract group)
    {
        _selectedGroup = group;
    }

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
            _selectedGroup = null;
            await LoadGroups();
        }
    }

    private void AddGroupedSetting()
    {
        if (_selectedGroup == null) return;

        _selectedGroup.GroupedSettings.Add(new GroupedSettingDataContract(
            "New Setting", null, "System.String", new List<SourceSettingDataContract>()));
        StateHasChanged();
    }

    private void RemoveGroupedSetting(GroupedSettingDataContract gs)
    {
        _selectedGroup?.GroupedSettings.Remove(gs);
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

        // Build a lookup of setting value types for type filtering in the dialog
        var settingValueTypes = SettingClientFacade.SettingClients
            .Where(c => !c.IsGroup && c.Instance == null)
            .SelectMany(c => c.Settings.Select(s => new { Key = $"{c.Name}|{s.Name}", ValueType = s.ValueType?.FullName ?? "System.String" }))
            .GroupBy(x => x.Key)
            .ToDictionary(g => g.Key, g => g.First().ValueType, StringComparer.OrdinalIgnoreCase);

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

            StateHasChanged();
        }
    }

    private void RemoveSourceSetting(GroupedSettingDataContract gs, SourceSettingDataContract ss)
    {
        gs.SourceSettings.Remove(ss);
        StateHasChanged();
    }
}
