using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Web.Models.Setting;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.ExtensionMethods;

public static class TypeExtensionMethods
{
    public static IDataGridValueModel ConvertToDataGridValueModel(this Type type,
        bool isReadOnly,
        ISetting parent,
        object? value = null,
        IEnumerable<string>? validValues = null,
        int? editorLineCount = null,
        string? validationRegex = null,
        string? validationExplanation = null)
    {
        //Console.WriteLine($"{type.FullName} -> {value} -> {value?.GetType()}");
        return type.FigPropertyType() switch
        {
            FigPropertyType.Int => new DataGridValueModel<int>((int?) (long?) value ?? 0, isReadOnly, parent, validationRegex: validationRegex, validationExplanation: validationExplanation),
            FigPropertyType.String when validValues != null => new DataGridValueModel<string>((string?) value ?? string.Empty, isReadOnly, parent, validValues),
            FigPropertyType.String => new DataGridValueModel<string>((string?) value ?? string.Empty, isReadOnly, parent, validValues, editorLineCount, validationRegex, validationExplanation),
            FigPropertyType.DateTime => new DataGridValueModel<DateTime>((DateTime?) value ?? DateTime.Now, isReadOnly, parent),
            FigPropertyType.Long => new DataGridValueModel<long>((long?) value ?? 0, isReadOnly, parent, validationRegex: validationRegex, validationExplanation: validationExplanation),
            FigPropertyType.Double => new DataGridValueModel<double>((double?) value ??
                                                                     0, isReadOnly, parent, validationRegex: validationRegex, validationExplanation: validationExplanation), // TODO: maybe casting problem here.
            FigPropertyType.Bool => new DataGridValueModel<bool>((bool?) value ?? false, isReadOnly, parent),
            FigPropertyType.TimeSpan => new DataGridValueModel<TimeSpan>(GetTimeSpanValue(value), isReadOnly, parent),
            _ => throw new NotSupportedException($"Type {type.FullName} is not supported in a datagrid.")
        };
    }

    private static TimeSpan GetTimeSpanValue(object? value)
    {
        if (value == null)
            return TimeSpan.Zero;

        return TimeSpan.Parse((string)value);
    }
}