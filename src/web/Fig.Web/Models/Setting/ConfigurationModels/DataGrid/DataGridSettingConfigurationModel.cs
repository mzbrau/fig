using System.Globalization;
using System.Security.Cryptography;
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
    private const string SecretDiffMask = "******";
    private const string SecretDiffChangedMask = "****** (changed)";
    private static readonly string[] StableKeyColumnNames = ["Id", "Key", "PrimaryKey"];
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

        return ProjectRows(Value, gridValue => gridValue.ReadOnlyValue);
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
        var originalVal = GetOriginalValueForDiff().ToDataGridStringValue(1000);
        var currentVal = GetCurrentValueForDiff().ToDataGridStringValue(1000);

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
        return new DataGridSettingConfigurationModel(DefinitionDataContract, parent, Presentation)
        {
            IsDirty = setDirty
        };
    }

    private List<Dictionary<string, object?>> GetCurrentValueForDiff()
    {
        var originalRowsByStableIdentifier = BuildOriginalRowsByStableIdentifier(OriginalValue);
        var originalRowsByCurrentIndex = MatchCurrentRowsToOriginalRowsByStableIdentifier(Value, originalRowsByStableIdentifier);

        return ProjectRows(Value, (rowIndex, columnName, gridValue) =>
        {
            if (!gridValue.IsSecret)
                return gridValue.ReadOnlyValue;

            var originalValue = GetOriginalReadOnlyValue(rowIndex, columnName, originalRowsByCurrentIndex);
            return Equals(originalValue, gridValue.ReadOnlyValue)
                ? SecretDiffMask
                : SecretDiffChangedMask;
        });
    }

    private List<Dictionary<string, object?>> GetOriginalValueForDiff()
    {
        return ProjectRows(OriginalValue, gridValue => gridValue.IsSecret ? SecretDiffMask : gridValue.ReadOnlyValue);
    }

    private static List<Dictionary<string, object?>> ProjectRows(
        List<Dictionary<string, IDataGridValueModel>>? rows,
        Func<IDataGridValueModel, object?> valueSelector)
    {
        return ProjectRows(rows, (_, _, gridValue) => valueSelector(gridValue));
    }

    private static List<Dictionary<string, object?>> ProjectRows(
        List<Dictionary<string, IDataGridValueModel>>? rows,
        Func<int, string, IDataGridValueModel, object?> valueSelector)
    {
        if (rows == null)
            return new List<Dictionary<string, object?>>();

        return rows
            .Select((row, rowIndex) => row.ToDictionary(
                keySelector: column => column.Key,
                elementSelector: column => valueSelector(rowIndex, column.Key, column.Value)))
            .ToList();
    }

    private object? GetOriginalReadOnlyValue(
        int rowIndex,
        string columnName,
        IReadOnlyDictionary<int, Dictionary<string, IDataGridValueModel>>? originalRowsByCurrentIndex = null)
    {
        if (originalRowsByCurrentIndex != null &&
            originalRowsByCurrentIndex.TryGetValue(rowIndex, out var matchedOriginalRow))
        {
            return matchedOriginalRow.TryGetValue(columnName, out var matchedOriginalValueModel)
                ? matchedOriginalValueModel.ReadOnlyValue
                : null;
        }

        if (OriginalValue == null || rowIndex < 0 || rowIndex >= OriginalValue.Count)
            return null;

        return OriginalValue[rowIndex].TryGetValue(columnName, out var originalValueModel)
            ? originalValueModel.ReadOnlyValue
            : null;
    }

    private static Dictionary<string, Queue<Dictionary<string, IDataGridValueModel>>> BuildOriginalRowsByStableIdentifier(
        List<Dictionary<string, IDataGridValueModel>>? rows)
    {
        var rowsByIdentifier = new Dictionary<string, Queue<Dictionary<string, IDataGridValueModel>>>(StringComparer.Ordinal);
        if (rows == null)
            return rowsByIdentifier;

        foreach (var row in rows)
        {
            if (!TryGetStableRowIdentifier(row, out var stableIdentifier))
                continue;

            if (!rowsByIdentifier.TryGetValue(stableIdentifier, out var bucket))
            {
                bucket = new Queue<Dictionary<string, IDataGridValueModel>>();
                rowsByIdentifier[stableIdentifier] = bucket;
            }

            bucket.Enqueue(row);
        }

        return rowsByIdentifier;
    }

    private static Dictionary<int, Dictionary<string, IDataGridValueModel>> MatchCurrentRowsToOriginalRowsByStableIdentifier(
        List<Dictionary<string, IDataGridValueModel>>? currentRows,
        Dictionary<string, Queue<Dictionary<string, IDataGridValueModel>>> originalRowsByStableIdentifier)
    {
        var matchedRowsByCurrentIndex = new Dictionary<int, Dictionary<string, IDataGridValueModel>>();
        if (currentRows == null || originalRowsByStableIdentifier.Count == 0)
            return matchedRowsByCurrentIndex;

        for (int rowIndex = 0; rowIndex < currentRows.Count; rowIndex++)
        {
            var currentRow = currentRows[rowIndex];
            if (!TryGetStableRowIdentifier(currentRow, out var stableIdentifier))
                continue;

            if (!originalRowsByStableIdentifier.TryGetValue(stableIdentifier, out var bucket) || bucket.Count == 0)
                continue;

            matchedRowsByCurrentIndex[rowIndex] = bucket.Dequeue();
        }

        return matchedRowsByCurrentIndex;
    }

    private static bool TryGetStableRowIdentifier(Dictionary<string, IDataGridValueModel> row, out string stableIdentifier)
    {
        var keyColumn = row.FirstOrDefault(column =>
            !column.Value.IsSecret &&
            StableKeyColumnNames.Contains(column.Key, StringComparer.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(keyColumn.Key))
        {
            var keyValue = Convert.ToString(keyColumn.Value.ReadOnlyValue, CultureInfo.InvariantCulture);
            if (!string.IsNullOrWhiteSpace(keyValue))
            {
                stableIdentifier = $"key:{keyColumn.Key}:{keyValue}";
                return true;
            }
        }

        var canonicalNonSecretRow = string.Join("|", row
            .Where(column => !column.Value.IsSecret)
            .OrderBy(column => column.Key, StringComparer.Ordinal)
            .Select(column =>
            {
                var value = Convert.ToString(column.Value.ReadOnlyValue, CultureInfo.InvariantCulture) ?? "<null>";
                return $"{column.Key}={value}";
            }));

        if (string.IsNullOrEmpty(canonicalNonSecretRow))
        {
            stableIdentifier = string.Empty;
            return false;
        }

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonicalNonSecretRow));
        stableIdentifier = $"hash:{Convert.ToHexString(hashBytes)}";
        return true;
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