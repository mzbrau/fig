namespace Fig.Api.Utils;

public interface IValidValuesHandler
{
    List<string>? GetValidValues(IList<string>? validValuesProperty, string commonEnumerationKey, Type valueType,
        dynamic value);

    dynamic? GetValue(dynamic value, Type valueType, IList<string>? validValuesProperty, string? commonEnumerationKey);

    dynamic GetValueFromValidValues(dynamic value, IList<string> validValues);
}