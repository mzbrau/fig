using System.Text;
using System.Text.RegularExpressions;
using Fig.Common.NetStandard.Json;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.ExtensionMethods;
using Newtonsoft.Json;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class
    DataGridSettingConfigurationModel : SettingConfigurationModel<List<Dictionary<string, IDataGridValueModel>>>
{
    private string _originalJson;

    public DataGridSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, bool isReadOnly)
        : base(dataContract, parent, isReadOnly)
    {
        DataGridConfiguration = new DataGridConfigurationModel(dataContract.DataGridDefinition!);
        Value ??= new List<Dictionary<string, IDataGridValueModel>>();
        OriginalValue ??= new List<Dictionary<string, IDataGridValueModel>>();
        _originalJson = JsonConvert.SerializeObject(OriginalValue, JsonSettings.FigDefault);
        
        ValidateDataGrid();
    }

    protected override object? GetValue(bool formatAsT = false)
    {
        if (formatAsT)
            return Value;

        var result = new List<Dictionary<string, object?>>();

        if (Value == null)
            return result;

        foreach (var row in Value)
        {
            var column = row.ToDictionary(
                a => a.Key,
                b => b.Value.ReadOnlyValue);
            result.Add(column);
        }

        return result;
    }

    public override string GetStringValue()
    {
        var rows = GetValue() as List<Dictionary<string, object?>>;
        
        if (rows is null || !rows.Any())
            return "<NOT SET>";

        var builder = new StringBuilder();
        foreach (var row in rows.Take(3))
        {
            builder.AppendLine(string.Join(",", row.Values));
        }

        if (rows.Count > 3)
            builder.AppendLine($"--{rows.Count - 3} more row(s) not shown--");

        return builder.ToString();
    }

    public override void EvaluateDirty()
    {
        var currentJson = JsonConvert.SerializeObject(Value, JsonSettings.FigDefault);
        IsDirty = _originalJson != currentJson;
    }

    public void ValidateDataGrid()
    {
        if (Value is null)
        {
            IsValid = true;
            return;
        }

        var validationErrors = new List<string>();
        foreach (var row in Value)
        {
            foreach (var column in row)
            {
                if (column.Value.ValidationRegex != null)
                {
                    var isValid = Regex.IsMatch(column.Value.ReadOnlyValue?.ToString() ?? string.Empty,
                        column.Value.ValidationRegex);
                    if (!isValid)
                    {
                        validationErrors.Add($"[{column.Key} - '{column.Value.ReadOnlyValue}'] {column.Value.ValidationExplanation}");
                    }
                }
            }
        }

        if (validationErrors.Any())
        {
            IsValid = false;
            var additionalErrorsMessage =
                validationErrors.Count > 1 ? $"(and {validationErrors.Count - 1} other error(s))" : string.Empty;

            ValidationExplanation = $"{validationErrors.First()} {additionalErrorsMessage}";
        }
        else
        {
            IsValid = true;
        }
    }

    protected override void Validate(string? value)
    {
        // Data grid validates differently.
    }

    public override void MarkAsSaved()
    {
        _originalJson = JsonConvert.SerializeObject(GetValue(true), JsonSettings.FigDefault);
        base.MarkAsSaved();
    }

    protected override void EvaluateDirty(List<Dictionary<string, IDataGridValueModel>>? value)
    {
        EvaluateDirty();
    }

    public override void ResetToDefault()
    {
        if (DefinitionDataContract.DefaultValue?.GetValue() != null)
        {
            Value = (List<Dictionary<string, IDataGridValueModel>>)DefinitionDataContract.GetEditableValue(true);
        }
        else
        {
            Value?.Clear();
            EvaluateDirty();
        }
    }

    public override SettingValueBaseDataContract? GetValueDataContract()
    {
        if (Value == null)
            return new DataGridSettingDataContract(null);

        var result = Value.Select(item => 
            item.ToDictionary(val => 
                val.Key, val => 
                val.Value.ReadOnlyValue))
            .ToList();
        
        return ValueDataContractFactory.CreateContract(result, typeof(List<Dictionary<string, object?>>));
    }

    

    public override ISetting Clone(SettingClientConfigurationModel parent, bool setDirty, bool isReadOnly)
    {
        return new DataGridSettingConfigurationModel(DefinitionDataContract, parent, isReadOnly)
        {
            IsDirty = setDirty
        };
    }
}