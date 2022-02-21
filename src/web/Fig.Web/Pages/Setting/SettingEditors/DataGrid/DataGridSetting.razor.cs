using Fig.Web.Models;
using Microsoft.AspNetCore.Components;
using Radzen.Blazor;

namespace Fig.Web.Pages.Setting.SettingEditors.DataGrid;

public partial class DataGridSetting
{
    RadzenDataGrid<Dictionary<string, IDataGridValueModel>> settingGrid;
    
    [Parameter]
    public DataGridSettingConfigurationModel Setting { get; set; }
    
    private void SetValue(Dictionary<string, object> context, string key, object value)
    {
        context[key] = value;
    }
    
    private async Task EditRow(Dictionary<string, IDataGridValueModel> row)
    {
        await settingGrid.EditRow(row);
    }
    
    private async Task SaveRow(Dictionary<string, IDataGridValueModel> row)
    {
        foreach (var item in row.Values)
        {
            item.RowSaved();
        }
        
        await settingGrid.UpdateRow(row);
        Setting.EvaluateDirty();
    }

    private void CancelEdit(Dictionary<string, IDataGridValueModel> row)
    {
        foreach (var item in row.Values)
        {
            item.RevertRowChanged();
        }

        settingGrid.CancelEditRow(row);
    }

    private async Task DeleteRow(Dictionary<string, IDataGridValueModel> row)
    {
        Setting.Value.Remove(row);
        await settingGrid.Reload();
        Setting.EvaluateDirty();
    }
    
    private async Task InsertRow()
    {
        var rowToInsert = Setting.DataGridConfiguration.CreateRow();
        Setting.Value.Add(rowToInsert);
        await settingGrid.Reload();
        Setting.EvaluateDirty();
        await EditRow(rowToInsert);
    }
}