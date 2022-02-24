using Fig.Contracts;
using Fig.Contracts.ExtensionMethods;
using Fig.Web.Models.Setting.ConfigurationModels.DataGrid;

namespace Fig.Web.ExtensionMethods;

public static class TypeExtensionMethods
{
    public static IDataGridValueModel ConvertToDataGridValueModel(this Type type, object? value = null)
    {
        Console.WriteLine($"{type.FullName} -> {value} -> {value?.GetType()}");
        return type.FigPropertyType() switch
        {
            FigPropertyType.Int => new DataGridValueModel<int>((int?) (long?) value ?? 0),
            FigPropertyType.String => new DataGridValueModel<string>((string?) value ?? string.Empty),
            FigPropertyType.DateTime => new DataGridValueModel<DateTime>((DateTime?) value ?? DateTime.Now),
            FigPropertyType.Long => new DataGridValueModel<long>((long?) value ?? 0),
            FigPropertyType.Double => new DataGridValueModel<double>((double?) value ??
                                                                     0), // TODO: maybe casting problem here.
            FigPropertyType.Bool => new DataGridValueModel<bool>((bool?) value ?? false),
            FigPropertyType.TimeSpan => new DataGridValueModel<TimeSpan>((TimeSpan?) value ?? TimeSpan.Zero),
            _ => throw new NotSupportedException($"Type {type.FullName} is not supported in a datagrid.")
        };
    }
}