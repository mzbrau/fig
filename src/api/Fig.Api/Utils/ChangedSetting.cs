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
}