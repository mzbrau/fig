using System.Globalization;
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
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
        DataGridConfiguration = new DataGridConfigurationModel(dataContract);
        Value ??= new List<Dictionary<string, IDataGridValueModel>>();
        OriginalValue ??= new List<Dictionary<string, IDataGridValueModel>>();
        _originalJson = JsonConvert.SerializeObject(OriginalValue, JsonSettings.FigDefault);
        
        ValidateDataGrid();
    }

    public override object? GetValue(bool formatAsT = false)
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
        return (GetValue() as List<Dictionary<string, object?>>).ToDataGridStringValue();
    }
    
    public override string GetChangeDiff()
    {
        var originalVal = GetOriginalValue().ToDataGridStringValue(1000);
        var currentVal = (GetValue() as List<Dictionary<string, object?>>).ToDataGridStringValue(1000);

        string[] lines1 = originalVal.Split('\n');
        string[] lines2 = currentVal.Split('\n');

        StringBuilder diffOutput = new StringBuilder();
        List<string> addedLines = new List<string>();
        List<string> removedLines = new List<string>();

        int index1 = 0;
        int index2 = 0;

        while (index1 < lines1.Length && index2 < lines2.Length)
        {
            if (lines1[index1].TrimEnd() == lines2[index2].TrimEnd())
            {
                index1++;
                index2++;
            }
            else
            {
                bool foundMatch = false;

                for (int i = index2 + 1; i < lines2.Length; i++)
                {
                    if (lines1[index1].TrimEnd() == lines2[i].TrimEnd())
                    {
                        for (int j = index2; j < i; j++)
                        {
                            addedLines.Add(lines2[j].TrimEnd());
                        }
                        index2 = i + 1;
                        foundMatch = true;
                        break;
                    }
                }

                if (!foundMatch)
                {
                    removedLines.Add(lines1[index1].TrimEnd());
                    index1++;
                }
            }
        }

        for (int i = index2; i < lines2.Length; i++)
        {
            addedLines.Add(lines2[i].TrimEnd());
        }

        foreach (string line in removedLines)
        {
            diffOutput.AppendLine($"-  {line}");
        }

        foreach (string line in addedLines)
        {
            diffOutput.AppendLine($"+ {line}");
        }

        return diffOutput.ToString();
    }

    public override void EvaluateDirty()
    {
        var currentJson = JsonConvert.SerializeObject(Value, JsonSettings.FigDefault);
        IsDirty = _originalJson != currentJson;
        UpdateBaseValueComparison();
        NotifySubscribers(ActionType.ValueChanged);
    }

    public void ValidateDataGrid(Action<int, string, string?>? processValidationError = null)
    {
        if (Value is null)
        {
            IsValid = true;
            return;
        }

        var validationErrors = new List<string>();
        int rowIndex = 0;
        foreach (var row in Value)
        {
            foreach (var column in row)
            {
                if (column.Value.ValidationRegex != null)
                {
                    var isValid = Regex.IsMatch(Convert.ToString(column.Value.ReadOnlyValue, CultureInfo.InvariantCulture) ?? string.Empty,
                        column.Value.ValidationRegex);
                    if (!isValid)
                    {
                        validationErrors.Add($"[{column.Key} - '{column.Value.ReadOnlyValue}'] {column.Value.ValidationExplanation}");
                        processValidationError?.Invoke(rowIndex, column.Key, column.Value.ValidationExplanation);
                    }
                }
            }

            rowIndex++;
        }

        if (validationErrors.Any())
        {
            IsValid = false;
            var additionalErrorsMessage =
                validationErrors.Count > 1 ? $" (and {validationErrors.Count - 1} other error(s))" : string.Empty;

            ValidationExplanation = $"{validationErrors.First()}{additionalErrorsMessage}";
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
            Value = (List<Dictionary<string, IDataGridValueModel>>?)DefinitionDataContract.GetEditableValue(this, true);
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
        return new DataGridSettingConfigurationModel(DefinitionDataContract, parent, _presentation)
        {
            IsDirty = setDirty
        };
    }
    
    private List<Dictionary<string, object?>> GetOriginalValue()
    {
        var result = new List<Dictionary<string, object?>>();

        if (OriginalValue == null)
            return result;

        foreach (var row in OriginalValue)
        {
            var column = row.ToDictionary(
                a => a.Key,
                b => b.Value.ReadOnlyValue);
            result.Add(column);
        }

        return result;
    }
}