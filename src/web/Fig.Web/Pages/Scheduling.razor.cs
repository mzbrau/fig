using Fig.Contracts.Scheduling;
using Fig.Web.Facades;
using Fig.Web.Models.Scheduling;
using Microsoft.AspNetCore.Components;
using Radzen;
using Radzen.Blazor;

namespace Fig.Web.Pages;

public partial class Scheduling
{
    [Inject]
    private ISchedulingFacade SchedulingFacade { get; set; } = null!;
    
    [Inject]
    private DialogService DialogService { get; set; } = null!;

    private bool _isRefreshInProgress;
    private RadzenDataGrid<DeferredChangeModel> _deferredChangesGrid = default!;

    private List<DeferredChangeModel> DeferredChanges => SchedulingFacade.DeferredChanges;
    
    protected override async Task OnInitializedAsync()
    {
        await Refresh();
        await base.OnInitializedAsync();
    }

    private async Task OnRefresh()
    {
        await Refresh();
    }

    private async Task Refresh()
    {
        _isRefreshInProgress = true;
        try
        {
            await SchedulingFacade.GetAllDeferredChanges();
            await _deferredChangesGrid.Reload();
        }
        finally
        {
            _isRefreshInProgress = false;
        }
    }
    
    private async Task EditRow(DeferredChangeModel row)
    {
        await _deferredChangesGrid.EditRow(row);
    }

    private async Task SaveRow(DeferredChangeModel row)
    {
        await SchedulingFacade.RescheduleChange(row.Id,
            new RescheduleDeferredChangeDataContract
            {
                NewExecuteAtUtc = row.ExecuteAtUtc
            });
        row.Save();
        await _deferredChangesGrid.UpdateRow(row);
    }

    private void CancelEdit(DeferredChangeModel row)
    {
        row.Revert();
        _deferredChangesGrid.CancelEditRow(row);
    }

    private async Task DeleteRow(DeferredChangeModel row)
    {
        if (await GetDeleteConfirmation())
        {
            await SchedulingFacade.DeleteDeferredChange(row.Id);
            await _deferredChangesGrid.Reload();
        }
    }
}