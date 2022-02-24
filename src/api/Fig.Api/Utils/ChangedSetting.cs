using System.Text;
using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;

namespace Fig.Api.Utils;

public class ChangedSetting
{
    public ChangedSetting(string name, object originalValue, object newValue, Type valueType, bool isSecret)
    {
        Name = name;
        if (isSecret)
        {
            OriginalValue = "<SECRET>";
            NewValue = "<SECRET>";
            ValueType = typeof(string);
        }
        else if (valueType.Is(FigPropertyType.DataGrid))
        {
            OriginalValue = GetDataGridValue(originalValue);
            NewValue = GetDataGridValue(newValue);
            ValueType = typeof(string);
        }
        else
        {
            OriginalValue = originalValue;
            NewValue = newValue;
            ValueType = valueType;
        }
    }

    public string Name { get; }

    public object OriginalValue { get; }

    public object NewValue { get; }

    public Type ValueType { get; }

    public static string GetDataGridValue(object value)
    {
        var list = value as List<Dictionary<string, object>>;

        if (list == null)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var row in list)
            builder.AppendLine(string.Join(",", row.Values));

        return builder.ToString();
    }
}