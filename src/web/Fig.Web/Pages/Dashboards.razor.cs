using Fig.Contracts.Authentication;
using Fig.Contracts.Dashboards;
using Fig.Web.Dashboards.Facades;
using Fig.Web.Notifications;
using Fig.Web.Pages.Dialogs;
using Fig.Web.Services;
using Microsoft.AspNetCore.Components;
using Radzen;

namespace Fig.Web.Pages;

public partial class Dashboards
{
    [Inject] private IDashboardFacade DashboardFacade { get; set; } = null!;
    [Inject] private IAccountService AccountService { get; set; } = null!;
    [Inject] private NavigationManager NavigationManager { get; set; } = null!;
    [Inject] private DialogService DialogService { get; set; } = null!;
    [Inject] private NotificationService NotificationService { get; set; } = null!;
    [Inject] private INotificationFactory NotificationFactory { get; set; } = null!;

    private List<DashboardDataContract> _dashboards = new();
    private bool _loading = true;

    private bool IsAdmin => AccountService.AuthenticatedUser?.Role == Role.Administrator;
    private bool IsDashboardRole => AccountService.AuthenticatedUser?.Role == Role.Dashboard;

    protected override async Task OnInitializedAsync()
    {
        await LoadDashboards();
        if (IsDashboardRole && _dashboards.Count == 1 && _dashboards[0].Id is Guid onlyId)
        {
            NavigationManager.NavigateTo($"/dashboards/{onlyId}");
            return;
        }

        await base.OnInitializedAsync();
    }

    private async Task LoadDashboards()
    {
        _loading = true;
        try
        {
            await DashboardFacade.LoadAll();
            _dashboards = DashboardFacade.Dashboards.OrderBy(d => d.Name).ToList();
        }
        finally
        {
            _loading = false;
        }
    }

    private void OpenView(DashboardDataContract dashboard)
    {
        if (dashboard.Id is Guid id)
            NavigationManager.NavigateTo($"/dashboards/{id}");
    }

    private void OpenEdit(Guid id)
    {
        NavigationManager.NavigateTo($"/dashboards/{id}/edit");
    }

    private async Task CreateDashboard()
    {
        var name = await DialogService.OpenAsync<TextPromptDialog>("New Dashboard",
            new Dictionary<string, object?> { { "Prompt", "Enter dashboard name:" } },
            new DialogOptions { Width = "420px" });

        if (name is not string dashboardName || string.IsNullOrWhiteSpace(dashboardName))
            return;

        var created = await DashboardFacade.Create(new DashboardDataContract
        {
            Name = dashboardName.Trim(),
            Definition = new DashboardDefinitionDataContract()
        });

        if (created?.Id is Guid id)
        {
            NotificationService.Notify(NotificationFactory.Success("Dashboard Created", $"'{created.Name}' created."));
            NavigationManager.NavigateTo($"/dashboards/{id}/edit");
            return;
        }

        NotificationService.Notify(NotificationFactory.Failure("Create Failed", "Could not create dashboard."));
    }

    private async Task EditProperties(DashboardDataContract dashboard)
    {
        if (dashboard.Id is not Guid id)
            return;

        dashboard.Definition ??= new DashboardDefinitionDataContract();
        dashboard.Definition.Refresh ??= new DashboardRefreshDataContract();

        var result = await DialogService.OpenAsync<DashboardPropertiesDialog>(
            "Dashboard Properties",
            new Dictionary<string, object?> { { nameof(DashboardPropertiesDialog.Dashboard), dashboard } },
            new DialogOptions { Width = "480px" });

        if (result is not DashboardPropertiesDialogResult props)
            return;

        dashboard.Name = props.Name;
        dashboard.Description = props.Description;
        dashboard.AdminOnly = props.AdminOnly;
        dashboard.Definition.Refresh.StatusSeconds = props.StatusSeconds;
        dashboard.Definition.Refresh.SettingsSeconds = props.SettingsSeconds;

        try
        {
            var updated = await DashboardFacade.Update(id, dashboard);
            if (updated is null)
            {
                NotificationService.Notify(NotificationFactory.Failure("Update Failed", "Could not update dashboard properties."));
                return;
            }

            NotificationService.Notify(NotificationFactory.Success("Updated", $"Dashboard '{updated.Name}' properties saved."));
            await LoadDashboards();
        }
        catch (DashboardConcurrencyConflictException)
        {
            NotificationService.Notify(NotificationFactory.Warning(
                "Conflict",
                "Another user modified this dashboard. Reload and try again."));
            await LoadDashboards();
        }
    }

    private async Task DuplicateDashboard(DashboardDataContract dashboard)
    {
        var name = await DialogService.OpenAsync<TextPromptDialog>("Duplicate Dashboard",
            new Dictionary<string, object?> { { "Prompt", "Name for the copy:" } },
            new DialogOptions { Width = "420px" });

        if (name is not string copyName || string.IsNullOrWhiteSpace(copyName))
            return;

        var clone = new DashboardDataContract
        {
            Name = copyName.Trim(),
            Description = dashboard.Description,
            AdminOnly = dashboard.AdminOnly,
            Definition = CloneDefinition(dashboard.Definition)
        };

        var created = await DashboardFacade.Create(clone);
        if (created is null)
        {
            NotificationService.Notify(NotificationFactory.Failure("Duplicate Failed", "Could not duplicate dashboard."));
            return;
        }

        NotificationService.Notify(NotificationFactory.Success("Duplicated", $"Created '{created.Name}'."));
        await LoadDashboards();
    }

    private async Task DeleteDashboard(DashboardDataContract dashboard)
    {
        if (dashboard.Id is not Guid id)
            return;

        var confirm = await DialogService.Confirm(
            $"Delete dashboard '{dashboard.Name}'? This cannot be undone.",
            "Delete Dashboard",
            new ConfirmOptions { OkButtonText = "Delete", CancelButtonText = "Cancel" });

        if (confirm != true)
            return;

        await DashboardFacade.Delete(id);
        NotificationService.Notify(NotificationFactory.Success("Deleted", $"Dashboard '{dashboard.Name}' deleted."));
        await LoadDashboards();
    }

    private static DashboardDefinitionDataContract CloneDefinition(DashboardDefinitionDataContract? source)
    {
        if (source is null)
            return new DashboardDefinitionDataContract();

        var json = Newtonsoft.Json.JsonConvert.SerializeObject(source);
        return Newtonsoft.Json.JsonConvert.DeserializeObject<DashboardDefinitionDataContract>(json)
               ?? new DashboardDefinitionDataContract();
    }
}
