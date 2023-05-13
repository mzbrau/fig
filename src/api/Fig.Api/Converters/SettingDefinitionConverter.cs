using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Converters;

public class SettingDefinitionConverter : ISettingDefinitionConverter
{
    private readonly IEncryptionService _encryptionService;
    private readonly ISettingVerificationConverter _settingVerificationConverter;
    private readonly IValidValuesHandler _validValuesHandler;
    private readonly ISettingConverter _settingConverter;

    public SettingDefinitionConverter(ISettingVerificationConverter settingVerificationConverter,
        IEncryptionService encryptionService, IValidValuesHandler validValuesHandler, ISettingConverter settingConverter)
    {
        _settingVerificationConverter = settingVerificationConverter;
        _encryptionService = encryptionService;
        _validValuesHandler = validValuesHandler;
        _settingConverter = settingConverter;
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
        var validValues = _validValuesHandler.GetValidValues(businessEntity.ValidValues,
            businessEntity.LookupTableKey, businessEntity.ValueType, businessEntity.Value);

        var defaultValue = validValues == null
            ? _settingConverter.Convert(businessEntity.DefaultValue)
            : new StringSettingDataContract(businessEntity.DefaultValue?.GetValue()?.ToString());
        
        return new SettingDefinitionDataContract(businessEntity.Name,
            businessEntity.Description,
            GetValue(businessEntity, validValues),
            businessEntity.IsSecret,
            validValues != null ? typeof(string) : businessEntity.ValueType,
            defaultValue,
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
    }

    private SettingValueBaseDataContract? GetValue(SettingBusinessEntity setting, IList<string>? validValues)
    {
        if (setting.IsSecret)
        {
            var encryptedValue = _encryptionService.Encrypt(setting?.Value?.GetValue()?.ToString());
            return new StringSettingDataContract(encryptedValue);
        }

        var value = validValues?.Any() == true
            ? _validValuesHandler.GetValueFromValidValues(setting.Value?.GetValue(), validValues)
            : setting.Value;

        return _settingConverter.Convert(value);
    }

    private SettingBusinessEntity Convert(SettingDefinitionDataContract dataContract)
    {
        return new SettingBusinessEntity
        {
            Name = dataContract.Name,
            Description = dataContract.Description,
            IsSecret = dataContract.IsSecret,
            ValueType = dataContract.ValueType,
            Value = _settingConverter.Convert(dataContract.DefaultValue),
            DefaultValue = _settingConverter.Convert(dataContract.DefaultValue),
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