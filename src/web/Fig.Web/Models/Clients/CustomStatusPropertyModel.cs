using Fig.Contracts.Status;

namespace Fig.Web.Models.Clients;

public class CustomStatusPropertyModel
{
    public CustomStatusPropertyModel(
        string name,
        string? displayName,
        CustomStatusValueType valueType,
        object? value,
        string? enumTypeName,
        bool highlight,
        bool showInUi,
        int order,
        string formattedValue,
        string? textColor = null)
    {
        Name = name;
        DisplayName = displayName;
        ValueType = valueType;
        Value = value;
        EnumTypeName = enumTypeName;
        Highlight = highlight;
        ShowInUi = showInUi;
        Order = order;
        FormattedValue = formattedValue;
        TextColor = textColor;
        CssTextColor = CustomStatusPropertiesValidator.IsValidTextColor(textColor) ? textColor : null;
    }

    public string Name { get; }

    public string? DisplayName { get; }

    public string Label => string.IsNullOrWhiteSpace(DisplayName) ? Name : DisplayName;

    public CustomStatusValueType ValueType { get; }

    public object? Value { get; }

    public string? EnumTypeName { get; }

    public bool Highlight { get; }

    public bool ShowInUi { get; }

    public int Order { get; }

    public string FormattedValue { get; }

    public string? TextColor { get; }

    /// <summary>
    /// Sanitized hex colour for CSS, or null when absent/invalid.
    /// </summary>
    public string? CssTextColor { get; }

    public string Summary => $"{Label}: {FormattedValue}";
}
