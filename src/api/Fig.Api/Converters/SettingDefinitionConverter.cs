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
        return new SettingsClientDefinitionDataContract
        {
            Name = businessEntity.Name,
            Instance = businessEntity.Instance,
            Settings = businessEntity.Settings.Select(Convert).ToList(),
            PluginVerifications = businessEntity.PluginVerifications
                .Select(verification => _settingVerificationConverter.Convert(verification))
                .ToList(),
            DynamicVerifications = businessEntity.DynamicVerifications
                .Select(verification => _settingVerificationConverter.Convert(verification))
                .ToList()
        };
    }

    private SettingDefinitionDataContract Convert(SettingBusinessEntity businessEntity)
    {
        var validValues = _validValuesBuilder.GetValidValues(businessEntity.ValidValues,
            businessEntity.CommonEnumerationKey, businessEntity.ValueType, businessEntity.Value);
        return new SettingDefinitionDataContract
        {
            Name = businessEntity.Name,
            Description = businessEntity.Description,
            IsSecret = businessEntity.IsSecret,
            Value = GetValue(businessEntity, validValues),
            DefaultValue = validValues == null ? businessEntity.DefaultValue : businessEntity.DefaultValue?.ToString(),
            ValueType = ResolveType(validValues, businessEntity.ValueType),
            ValidationType = Enum.Parse<ValidationType>(businessEntity.ValidationType),
            ValidationRegex = businessEntity.ValidationRegex,
            ValidationExplanation = businessEntity.ValidationExplanation,
            ValidValues = validValues,
            Group = businessEntity.Group,
            DisplayOrder = businessEntity.DisplayOrder,
            Advanced = businessEntity.Advanced,
            CommonEnumerationKey = businessEntity.CommonEnumerationKey,
            EditorLineCount = businessEntity.EditorLineCount,
            JsonSchema = businessEntity.JsonSchema,
            DataGridDefinition = businessEntity.DataGridDefinitionJson != null
                ? JsonConvert.DeserializeObject<DataGridDefinitionDataContract>(businessEntity.DataGridDefinitionJson)
                : null
        };

        Type ResolveType(List<string>? validValues, Type valueType)
        {
            if (validValues == null)
            {
                return valueType.IsGenericType ? typeof(List<string>) : typeof(string);
            }

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
            CommonEnumerationKey = dataContract.CommonEnumerationKey,
            EditorLineCount = dataContract.EditorLineCount,
            JsonSchema = dataContract.JsonSchema,
            DataGridDefinitionJson = dataContract.DataGridDefinition != null
                ? JsonConvert.SerializeObject(dataContract.DataGridDefinition)
                : null
        };
    }
}