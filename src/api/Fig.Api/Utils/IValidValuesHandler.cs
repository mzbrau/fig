namespace Fig.Api.Utils;

public interface IValidValuesHandler
{
    List<string>? GetValidValues(IList<string>? validValuesProperty, string lookupTableKey, Type valueType,
        dynamic value);

    dynamic? GetValue(dynamic value, Type valueType, IList<string>? validValuesProperty, string? lookupTableKey);

    dynamic GetValueFromValidValues(dynamic value, IList<string> validValues);
}