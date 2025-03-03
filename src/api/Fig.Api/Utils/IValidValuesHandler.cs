using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Utils;

public interface IValidValuesHandler
{
    Task<List<string>?> GetValidValues(IList<string>? validValuesProperty, string? lookupTableKey, Type? valueType, SettingValueBaseBusinessEntity? value);

    SettingValueBaseBusinessEntity? GetValue(SettingValueBaseBusinessEntity value, IList<string>? validValuesProperty, Type valueType, string? lookupTableKey, DataGridDefinitionDataContract? dataGridDefinition);

    SettingValueBaseBusinessEntity GetValueFromValidValues(object? value, IList<string> validValues,
        DataGridDefinitionDataContract? dataGridDefinition);
}