using Fig.Api.Encryption;
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
    private readonly IValidValuesHandler _validValuesBuilder;
    private readonly ISettingVerificationConverter _settingVerificationConverter;

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
        var validValues = _validValuesBuilder.GetValidValues(businessEntity.ValidValues, businessEntity.CommonEnumerationKey, businessEntity.ValueType, businessEntity.Value);
        return new SettingDefinitionDataContract
        {
            Name = businessEntity.Name,
            Description = businessEntity.Description,
            IsSecret = businessEntity.IsSecret,
            Value = GetValue(businessEntity, validValues),
            DefaultValue = validValues == null ? businessEntity.DefaultValue : businessEntity.DefaultValue?.ToString(),
            ValueType = validValues == null ? businessEntity.ValueType : typeof(string),
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
    }

    private dynamic? GetValue(SettingBusinessEntity businessEntity, IList<string>? validValues)
    {
        if (businessEntity.Value == null)
            return null;

        if (!businessEntity.IsSecret)
            return validValues == null ? businessEntity.Value : validValues.FirstOrDefault();

        EncryptionResultModel encryptionResult = _encryptionService.Encrypt(businessEntity.Value.ToString());
        return encryptionResult.EncryptedValue;
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