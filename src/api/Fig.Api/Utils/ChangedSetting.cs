using System.Text;
using Fig.Common.Constants;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Utils;

public class ChangedSetting
{
    public ChangedSetting(string name,
        SettingValueBaseBusinessEntity? originalValue,
        SettingValueBaseBusinessEntity? newValue,
        bool isSecret, DataGridDefinitionDataContract? dataGridDefinition)
    {
        Name = name;
        IsSecret = isSecret;
        if (isSecret)
        {
            OriginalValue = new StringSettingBusinessEntity(SecretConstants.SecretPlaceholder);
            NewValue = new StringSettingBusinessEntity(SecretConstants.SecretPlaceholder);
        }
        else if (newValue is DataGridSettingBusinessEntity newDataGridVal && 
                 originalValue is DataGridSettingBusinessEntity originalDataGridVal)
        {
            OriginalValue = GetDataGridValue(originalDataGridVal, dataGridDefinition);
            NewValue = GetDataGridValue(newDataGridVal, dataGridDefinition);
        }
        else
        {
            OriginalValue = originalValue;
            NewValue = newValue;
        }
    }

    public string Name { get; }
    
    public SettingValueBaseBusinessEntity? OriginalValue { get; }
    
    public SettingValueBaseBusinessEntity? NewValue { get; }
    
    public bool IsSecret { get; }

    public static StringSettingBusinessEntity GetDataGridValue(DataGridSettingBusinessEntity? value,
        DataGridDefinitionDataContract? dataGridDefinition)
    {
        var list = value?.Value;

        if (list == null || dataGridDefinition == null)
            return new StringSettingBusinessEntity(string.Empty);

        var builder = new StringBuilder();
        foreach (var row in list)
            builder.AppendLine(string.Join(",", GetRowValuesWithSecretRedaction(row, dataGridDefinition)));

        return new StringSettingBusinessEntity(builder.ToString());
    }

    private static IEnumerable<string?> GetRowValuesWithSecretRedaction(Dictionary<string,object?> row, DataGridDefinitionDataContract dataGridDefinition)
    {
        foreach (var column in dataGridDefinition.Columns)
        {
            if (column.IsSecret)
            {
                yield return SecretConstants.SecretPlaceholder;
            }
            else if (row.TryGetValue(column.Name, out var value))
            {
                yield return value?.ToString();
            }
        }
    }
}