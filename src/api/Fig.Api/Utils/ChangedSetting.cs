using System.Text;  
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Utils;

public class ChangedSetting
{
    public ChangedSetting(string name,
        SettingValueBaseBusinessEntity? originalValue,
        SettingValueBaseBusinessEntity? newValue,
        bool isSecret)
    {
        Name = name;
        if (isSecret)
        {
            OriginalValue = new StringSettingBusinessEntity("<SECRET>");
            NewValue = new StringSettingBusinessEntity("<SECRET>");
        }
        else if (newValue is DataGridSettingBusinessEntity)
        {
            OriginalValue = GetDataGridValue(originalValue);
            NewValue = GetDataGridValue(newValue);
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

    public static StringSettingBusinessEntity GetDataGridValue(object value)
    {
        var list = value as List<Dictionary<string, object>>;

        if (list == null)
            return new StringSettingBusinessEntity(string.Empty);

        var builder = new StringBuilder();
        foreach (var row in list)
            builder.AppendLine(string.Join(",", row.Values));

        return new StringSettingBusinessEntity(builder.ToString());
    }
}