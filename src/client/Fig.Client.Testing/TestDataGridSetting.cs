using System;
using System.Collections.Generic;
using System.Linq;
using Fig.Common.NetStandard.Scripting;

namespace Fig.Client.Testing;

/// <summary>
/// A test data grid setting implementation for testing display scripts
/// </summary>
public class TestDataGridSetting : TestSetting, IDataGridSettingModel
{
    public TestDataGridSetting(string name, List<Dictionary<string, IDataGridValueModel>>? initialValue = null)
        : base(name, typeof(List<Dictionary<string, IDataGridValueModel>>), initialValue)
    {
        Value = initialValue ?? new List<Dictionary<string, IDataGridValueModel>>();
    }
    
    public IDataGridConfigurationModel? DataGridConfiguration { get; set; }
    
    public List<Dictionary<string, IDataGridValueModel>>? Value { get; set; }

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
                if (!string.IsNullOrEmpty(column.Value.ValidationRegex))
                {
                    var valueStr = Convert.ToString(column.Value.ReadOnlyValue) ?? string.Empty;
                    var isValid = System.Text.RegularExpressions.Regex.IsMatch(valueStr, column.Value.ValidationRegex!);
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

    public override object? GetValue(bool formatAsT = false) => Value;
}
