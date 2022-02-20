using Fig.Contracts;
using Fig.Web.Models;

namespace Fig.Web.ExtensionMethods;

public static class TypeExtensionMethods
{
    public static IDataGridValueModel ConvertToDataGridValueModel(this Type type, object? value = null)
    {
        Console.WriteLine($"{type.FullName} -> {value} -> {value?.GetType()}");
        return type.FullName switch
        {
            SupportedTypes.Int => new DataGridValueModel<int>((int?)(long?) value ?? 0),
            SupportedTypes.String => new DataGridValueModel<string>((string?) value ?? string.Empty),
            SupportedTypes.DateTime => new DataGridValueModel<DateTime>((DateTime?) value ?? DateTime.Now),
            SupportedTypes.Long => new DataGridValueModel<long>((long?) value ?? 0),
            SupportedTypes.Double => new DataGridValueModel<double>((double?) value ?? 0), // TODO: maybe casting problem here.
            SupportedTypes.DateOnly => new DataGridValueModel<DateOnly>((DateOnly?) value ?? DateOnly.FromDateTime(DateTime.Now)),
            SupportedTypes.TimeOnly => new DataGridValueModel<TimeOnly>((TimeOnly?) value ?? TimeOnly.FromDateTime(DateTime.Now)),
            SupportedTypes.Bool => new DataGridValueModel<bool>((bool?) value ?? false),
            SupportedTypes.TimeSpan => new DataGridValueModel<TimeSpan>((TimeSpan?) value ?? TimeSpan.Zero),
            _ => throw new NotSupportedException($"Type {type.FullName} is not supported in a datagrid.")
        };
    }
}