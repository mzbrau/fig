using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;
using Microsoft.AspNetCore.Components;
using Namotion.Reflection;
using Radzen.Blazor;

namespace Fig.Web.Pages.Setting.SettingEditors.DataGrid;

public partial class DataGridSetting
{
    private RadzenDataGrid<Dictionary<string, IDataGridValueModel>> _settingGrid = null!;

    [Parameter]
    public DataGridSettingConfigurationModel Setting { get; set; } = null!;

    private void SetValue(Dictionary<string, object> context, string key, object value)
    {
        context[key] = value;
    }

    private async Task EditRow(Dictionary<string, IDataGridValueModel> row)
    {
        await _settingGrid.EditRow(row);
    }

    private async Task SaveRow(Dictionary<string, IDataGridValueModel> row)
    {
        foreach (var item in row.Values)
            item.RowSaved();

        await _settingGrid.UpdateRow(row);
        Setting.EvaluateDirty();
        await Task.Run(() => Setting.ValidateDataGrid());
    }

    private void CancelEdit(Dictionary<string, IDataGridValueModel> row)
    {
        foreach (var item in row.Values)
            item.RevertRowChanged();

        _settingGrid.CancelEditRow(row);
    }

    private async Task DeleteRow(Dictionary<string, IDataGridValueModel> row)
    {
        Setting.Value?.Remove(row);
        await _settingGrid.Reload();
        Setting.EvaluateDirty();
    }

    private async Task InsertRow()
    {
        var rowToInsert = Setting.DataGridConfiguration?.CreateRow();
        if (rowToInsert is not null)
        {
            Setting.Value?.Add(rowToInsert);
            await _settingGrid.Reload();
            Setting.EvaluateDirty();
            await EditRow(rowToInsert);
        }
    }
}