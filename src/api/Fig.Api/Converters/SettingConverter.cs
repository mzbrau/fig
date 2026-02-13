using Fig.Api.ExtensionMethods;
using Fig.Common.Constants;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Converters;

public class SettingConverter : ISettingConverter
{
    private readonly IValueToStringConverter _valueToStringConverter;

    public SettingConverter(IValueToStringConverter valueToStringConverter)
    {
        _valueToStringConverter = valueToStringConverter;
    }

    public SettingDataContract Convert(SettingBusinessEntity setting)
    {
        return new SettingDataContract(setting.Name, Convert(setting.Value, setting.HasSchema()), setting.IsSecret);
    }

    public SettingBusinessEntity Convert(SettingDataContract setting, SettingBusinessEntity? originalSetting)
    {
        return new SettingBusinessEntity
        {
            Name = setting.Name,
            Value = Convert(setting.Value, originalSetting)
        };
    }

    public SettingValueDataContract Convert(SettingValueBusinessEntity businessEntity)
    {
        
        return new SettingValueDataContract(businessEntity.SettingName,
            _valueToStringConverter.Convert(businessEntity.Value?.GetValue()),
            businessEntity.ChangedAt,
            businessEntity.ChangedBy,
            businessEntity.ChangeMessage);
    }

    public SettingValueBaseDataContract? Convert(SettingValueBaseBusinessEntity? value, bool hasSchema,
        DataGridDefinitionDataContract? dataGridDefinition = null)
    {
        if (value is null)
            return null;

        return value switch
        {
            StringSettingBusinessEntity s when hasSchema => new JsonSettingDataContract(s.Value),
            StringSettingBusinessEntity s => new StringSettingDataContract(s.Value),
            BoolSettingBusinessEntity s => new BoolSettingDataContract(s.Value),
            DataGridSettingBusinessEntity s => GetDataGridDataContract(s.Value, dataGridDefinition),
            DateTimeSettingBusinessEntity s => new DateTimeSettingDataContract(s.Value),
            DoubleSettingBusinessEntity s => new DoubleSettingDataContract(s.Value),
            IntSettingBusinessEntity s => new IntSettingDataContract(s.Value),
            TimeSpanSettingBusinessEntity s => new TimeSpanSettingDataContract(s.Value),
            LongSettingBusinessEntity s => new LongSettingDataContract(s.Value),
            _ => throw new NotImplementedException($"'{value?.GetType()}' is not implemented.")
        };
    }

    public SettingValueBaseBusinessEntity? Convert(SettingValueBaseDataContract? value,
        SettingBusinessEntity? originalSetting = null)
    {
        if (value is null)
            return null;
        
        return value switch
        {
            StringSettingDataContract s => new StringSettingBusinessEntity(s.Value),
            BoolSettingDataContract s => new BoolSettingBusinessEntity(s.Value),
            DataGridSettingDataContract s => GetDataGridBusinessEntity(s.Value, originalSetting),
            DateTimeSettingDataContract s => new DateTimeSettingBusinessEntity(s.Value),
            DoubleSettingDataContract s => new DoubleSettingBusinessEntity(s.Value),
            IntSettingDataContract s => new IntSettingBusinessEntity(s.Value),
            TimeSpanSettingDataContract s => new TimeSpanSettingBusinessEntity(s.Value),
            LongSettingDataContract s => new LongSettingBusinessEntity(s.Value),
            JsonSettingDataContract s => new StringSettingBusinessEntity(s.Value),
            _ => throw new NotImplementedException($"'{value.GetType()}' is not implemented.")
        };
    }

    private SettingValueBaseBusinessEntity? GetDataGridBusinessEntity(List<Dictionary<string, object?>>? value,
        SettingBusinessEntity? originalSetting)
    {
        if (originalSetting?.DataGridDefinitionJson is null || originalSetting.Value?.GetValue() is null)
            return new DataGridSettingBusinessEntity(value);
        
        var originalValue = originalSetting.Value?.GetValue() as List<Dictionary<string, object?>>;
        foreach (var column in originalSetting.GetDataGridDefinition()?.Columns.Where(a => a.IsSecret) ?? [])
        {
            for (var i = 0; i < (value?.Count ?? 0); i++)
            {
                var currentRow = value?[i];
                if (currentRow is null || !currentRow.TryGetValue(column.Name, out var currentValue))
                    continue;

                if (currentValue is SecretConstants.SecretPlaceholder)
                {
                    object? original = null;
                    if (originalValue is not null && originalValue.Count > i)
                    {
                        var originalRow = originalValue[i];
                        originalRow.TryGetValue(column.Name, out original);
                    }

                    currentRow[column.Name] = original;
                }
            }
        }
        
        return new DataGridSettingBusinessEntity(value);
    }

    private SettingValueBaseDataContract? GetDataGridDataContract(List<Dictionary<string,object?>>? value, DataGridDefinitionDataContract? dataGridDefinition)
    {
        foreach (var column in dataGridDefinition?.Columns.Where(a => a.IsSecret) ?? [])
        {
            foreach (var row in value ?? [])
            {
                if (row.TryGetValue(column.Name, out var val) && 
                    val is string strValue && 
                    !string.IsNullOrWhiteSpace(strValue))
                {
                    row[column.Name] = SecretConstants.SecretPlaceholder;
                }
            }
        }

        return new DataGridSettingDataContract(value);
    }
}