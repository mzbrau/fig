using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Fig.Common.NetStandard.Json;
using Fig.Common.NetStandard.Scripting;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Web.ExtensionMethods;
using Newtonsoft.Json;

namespace Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

public class DataGridSettingConfigurationModel : SettingConfigurationModel<List<Dictionary<string, IDataGridValueModel>>>, IDataGridSettingModel
{
    private string _originalJson;
    private int _initialRowCount;
    private const int MaxRowsForValidation = 10;

    public DataGridSettingConfigurationModel(SettingDefinitionDataContract dataContract,
        SettingClientConfigurationModel parent, SettingPresentation presentation)
        : base(dataContract, parent, presentation)
    {
        DataGridConfiguration = new DataGridConfigurationModel(dataContract);
        Value ??= new List<Dictionary<string, IDataGridValueModel>>();
        OriginalValue ??= new List<Dictionary<string, IDataGridValueModel>>();
        _originalJson = JsonConvert.SerializeObject(OriginalValue, JsonSettings.FigDefault);
        _initialRowCount = Value?.Count ?? 0;
        
        // Lazy validation: Don't validate in constructor, validation will happen on first access
        // This significantly improves load times for settings with many data grid rows
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

    public override string IconKey => "list";

    public override string GetStringValue(int maxLength = 200)
    {
        return (GetValue() as List<Dictionary<string, object?>>).ToDataGridStringValue();
    }
    
    public Dictionary<string, IDataGridValueModel>? CreateRow(
        DataGridSettingConfigurationModel setting)
    {
        var model = DataGridConfiguration as DataGridConfigurationModel;
        return model?.CreateRow(setting);
    }
    
    public override string GetChangeDiff()
    {
        var originalVal = GetOriginalValue().ToDataGridStringValue(1000);
        var currentVal = (GetValue() as List<Dictionary<string, object?>>).ToDataGridStringValue(1000);

        string[] lines1 = originalVal.Split('\n');
        string[] lines2 = currentVal.Split('\n');

        // Remove empty lines
        var originalLines = lines1.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.TrimEnd()).ToList();
        var currentLines = lines2.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l.TrimEnd()).ToList();

        StringBuilder diffOutput = new StringBuilder();

        // Find lines that exist in original but not in current (pure removals)
        var removedLines = originalLines.Where(line => !currentLines.Contains(line)).ToList();
        
        // Find lines that exist in current but not in original (pure additions)
        var addedLines = currentLines.Where(line => !originalLines.Contains(line)).ToList();

        // Only show reorderings if there are no pure additions or removals
        // This prevents showing reorderings when items are just shifted due to insertions/deletions
        bool hasPureChanges = removedLines.Any() || addedLines.Any();

        if (!hasPureChanges)
        {
            // No pure additions/removals, so check for reorderings
            // Use a simple line-by-line comparison for true reorderings
            int maxLength = Math.Max(originalLines.Count, currentLines.Count);
            
            for (int i = 0; i < maxLength; i++)
            {
                string? originalLine = i < originalLines.Count ? originalLines[i] : null;
                string? currentLine = i < currentLines.Count ? currentLines[i] : null;

                if (originalLine != currentLine)
                {
                    if (!string.IsNullOrEmpty(originalLine))
                        diffOutput.AppendLine($"-  {originalLine}");
                    if (!string.IsNullOrEmpty(currentLine))
                        diffOutput.AppendLine($"+ {currentLine}");
                }
            }
        }
        else
        {
            // Show pure additions and removals only
            foreach (string line in removedLines)
            {
                diffOutput.AppendLine($"-  {line}");
            }

            foreach (string line in addedLines)
            {
                diffOutput.AppendLine($"+ {line}");
            }
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

        // Performance optimization: Skip validation if there are more than 10 rows
        // Only validate new rows that are added after initial load
        if (_initialRowCount > MaxRowsForValidation)
        {
            // Only validate rows added after the initial load
            var validationErrors = new List<string>();
            int rowIndex = _initialRowCount;
            
            for (int i = _initialRowCount; i < Value.Count; i++)
            {
                var row = Value[i];
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
            return;
        }

        // For data grids with 10 or fewer rows, validate all rows
        var allValidationErrors = new List<string>();
        int allRowIndex = 0;
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
                        allValidationErrors.Add($"[{column.Key} - '{column.Value.ReadOnlyValue}'] {column.Value.ValidationExplanation}");
                        processValidationError?.Invoke(allRowIndex, column.Key, column.Value.ValidationExplanation);
                    }
                }
            }

            allRowIndex++;
        }

        if (allValidationErrors.Any())
        {
            IsValid = false;
            var additionalErrorsMessage =
                allValidationErrors.Count > 1 ? $" (and {allValidationErrors.Count - 1} other error(s))" : string.Empty;

            ValidationExplanation = $"{allValidationErrors.First()}{additionalErrorsMessage}";
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

    public override void Initialize()
    {
        // Lazy validation: validate on first access
        // This is triggered the first time the setting is accessed/displayed
        if (!_hasBeenValidated)
        {
            ValidateDataGrid();
            _hasBeenValidated = true;
        }
        RunDisplayScript();
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
        return new DataGridSettingConfigurationModel(DefinitionDataContract, parent, Presentation)
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

    // Explicit interface implementation to handle type compatibility
    List<Dictionary<string, IDataGridValueModel>>? IDataGridSettingModel.Value 
    { 
        get => Value?.Select(row => row.ToDictionary(
            kvp => kvp.Key, 
            kvp => kvp.Value)).ToList();
        set => Value = value?.Select(row => row.ToDictionary(
            kvp => kvp.Key, 
            kvp => kvp.Value)).ToList();
    }
}