using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Converters;

public class SettingDefinitionConverter : ISettingDefinitionConverter
{
    private readonly IEncryptionService _encryptionService;
    private readonly ISettingVerificationConverter _settingVerificationConverter;
    private readonly IValidValuesHandler _validValuesBuilder;

    public SettingDefinitionConverter(ISettingVerificationConverter settingVerificationConverter,
        IEncryptionService encryptionService, IValidValuesHandler validValuesBuilder)
    {
        _settingVerificationConverter = settingVerificationConverter;
        _encryptionService = encryptionService;
        _validValuesBuilder = validValuesBuilder;
    }

    public SettingClientBusinessEntity Convert(SettingsClientDefinitionDataContract dataContract)
    {
        return new SettingClientBusinessEntity
        {
            Name = dataContract.Name,
            Settings = dataContract.Settings.Select(Convert).ToList(),
            PluginVerifications = dataContract.PluginVerifications
                .Select(verification => _settingVerificationConverter.Convert(verification))
                .ToList(),
            DynamicVerifications = dataContract.DynamicVerifications
                .Select(verification => _settingVerificationConverter.Convert(verification))
                .ToList()
        };
    }

    public SettingsClientDefinitionDataContract Convert(SettingClientBusinessEntity businessEntity)
    {
        return new SettingsClientDefinitionDataContract(businessEntity.Name,
            businessEntity.Instance,
            businessEntity.Settings.Select(Convert).ToList(),
            businessEntity.PluginVerifications
                .Select(verification => _settingVerificationConverter.Convert(verification))
                .ToList(),
            businessEntity.DynamicVerifications
                .Select(verification => _settingVerificationConverter.Convert(verification))
                .ToList());
    }

    private SettingDefinitionDataContract Convert(SettingBusinessEntity businessEntity)
    {
        var validValues = _validValuesBuilder.GetValidValues(businessEntity.ValidValues,
            businessEntity.LookupTableKey, businessEntity.ValueType, businessEntity.Value);
        
        return new SettingDefinitionDataContract(businessEntity.Name,
            businessEntity.Description,
            businessEntity.IsSecret,
            GetValue(businessEntity, validValues),
            validValues == null ? businessEntity.DefaultValue : businessEntity.DefaultValue?.ToString(),
            ResolveType(validValues, businessEntity.ValueType),
            Enum.Parse<ValidationType>(businessEntity.ValidationType),
            businessEntity.ValidationRegex,
            businessEntity.ValidationExplanation,
            validValues,
            businessEntity.Group,
            businessEntity.DisplayOrder,
            businessEntity.Advanced,
            businessEntity.LookupTableKey,
            businessEntity.EditorLineCount,
            businessEntity.JsonSchema,
            businessEntity.DataGridDefinitionJson != null
                ? JsonConvert.DeserializeObject<DataGridDefinitionDataContract>(businessEntity.DataGridDefinitionJson)
                : null,
            businessEntity.EnablesSettings,
            businessEntity.SupportsLiveUpdate);

        Type ResolveType(List<string>? validValues, Type valueType)
        {
            if (validValues != null && !valueType.IsGenericType)
                return typeof(string);

            return businessEntity.ValueType;
        }
    }

    private dynamic? GetValue(SettingBusinessEntity businessEntity, IList<string>? validValues)
    {
        if (businessEntity.Value == null)
            return null;

        if (businessEntity.IsSecret)
            return _encryptionService.Encrypt(businessEntity.Value.ToString());

        return validValues?.Any() == true
            ? _validValuesBuilder.GetValueFromValidValues(businessEntity.Value, validValues)
            : businessEntity.Value;
    }

    private SettingBusinessEntity Convert(SettingDefinitionDataContract dataContract)
    {
        return new SettingBusinessEntity
        {
            Name = dataContract.Name,
            Description = dataContract.Description,
            IsSecret = dataContract.IsSecret,
            Value = dataContract.DefaultValue,
            DefaultValue = dataContract.DefaultValue,
            ValueType = dataContract.ValueType,
            ValidationType = dataContract.ValidationType.ToString(),
            ValidationRegex = dataContract.ValidationRegex,
            ValidationExplanation = dataContract.ValidationExplanation,
            ValidValues = dataContract.ValidValues,
            Group = dataContract.Group,
            DisplayOrder = dataContract.DisplayOrder,
            Advanced = dataContract.Advanced,
            LookupTableKey = dataContract.LookupTableKey,
            EditorLineCount = dataContract.EditorLineCount,
            JsonSchema = dataContract.JsonSchema,
            DataGridDefinitionJson = dataContract.DataGridDefinition != null
                ? JsonConvert.SerializeObject(dataContract.DataGridDefinition)
                : null,
            EnablesSettings = dataContract.EnablesSettings,
            SupportsLiveUpdate = dataContract.SupportsLiveUpdate
        };
    }
}