using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Api.Validators;
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
            Description = dataContract.Description,
            Settings = dataContract.Settings.Select(Convert).ToList(),
            Verifications = dataContract.Verifications
                .Select(_settingVerificationConverter.Convert)
                .ToList(),
        };
    }

    public SettingsClientDefinitionDataContract Convert(SettingClientBusinessEntity businessEntity, bool allowDisplayScripts)
    {
        return new SettingsClientDefinitionDataContract(businessEntity.Name,
            businessEntity.Description,
            businessEntity.Instance,
            businessEntity.Settings.Any(a => !string.IsNullOrEmpty(a.DisplayScript)),
            businessEntity.Settings.Select(s => Convert(s, allowDisplayScripts)).ToList(),
            businessEntity.Verifications
                .Select(_settingVerificationConverter.Convert)
                .ToList(),
            new List<SettingDataContract>());
    }

    private SettingDefinitionDataContract Convert(SettingBusinessEntity businessEntity, bool allowDisplayScripts)
    {
        var dataGridDefinition = businessEntity.GetDataGridDefinition();
        List<string>? validValues = null;
        if (dataGridDefinition is null)
        {
            validValues = _validValuesHandler.GetValidValues(businessEntity.ValidValues,
                businessEntity.LookupTableKey, businessEntity.ValueType, businessEntity.Value);
        }
        else if (dataGridDefinition.Columns.Count == 1)
        {
            var firstColumn = dataGridDefinition.Columns.First();
            validValues = firstColumn.ValidValues = _validValuesHandler.GetValidValues(firstColumn.ValidValues,
                businessEntity.LookupTableKey, firstColumn.ValueType, businessEntity.Value);
            firstColumn.ValidValues = validValues;
        }

        var defaultValue = validValues == null
            ? _settingConverter.Convert(businessEntity.DefaultValue, businessEntity.HasSchema(), dataGridDefinition)
            : new StringSettingDataContract(businessEntity.DefaultValue?.GetValue()?.ToString());
        
        return new SettingDefinitionDataContract(businessEntity.Name,
            businessEntity.Description,
            GetValue(businessEntity, validValues, dataGridDefinition),
            businessEntity.IsSecret,
            validValues != null && dataGridDefinition is null ? typeof(string) : businessEntity.ValueType,
            defaultValue,
            businessEntity.ValidationRegex,
            businessEntity.ValidationExplanation,
            validValues,
            businessEntity.Group,
            businessEntity.DisplayOrder,
            businessEntity.Advanced,
            businessEntity.LookupTableKey,
            businessEntity.EditorLineCount,
            businessEntity.JsonSchema,
            dataGridDefinition,
            businessEntity.EnablesSettings,
            businessEntity.SupportsLiveUpdate,
            businessEntity.LastChanged,
            businessEntity.CategoryColor,
            businessEntity.CategoryName,
            allowDisplayScripts ? businessEntity.DisplayScript : null);
    }

    private SettingValueBaseDataContract? GetValue(SettingBusinessEntity setting, IList<string>? validValues,
        DataGridDefinitionDataContract? dataGridDefinition)
    {
        if (setting.IsSecret)
        {
            var encryptedValue = _encryptionService.Encrypt(setting?.Value?.GetValue()?.ToString());
            return new StringSettingDataContract(encryptedValue);
        }

        var value = validValues?.Any() == true
            ? _validValuesHandler.GetValueFromValidValues(setting.Value?.GetValue(), validValues)
            : setting.Value;

        return _settingConverter.Convert(value, setting.HasSchema(), dataGridDefinition);
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
            SupportsLiveUpdate = dataContract.SupportsLiveUpdate,
            LastChanged = dataContract.LastChanged,
            CategoryColor = dataContract.CategoryColor,
            CategoryName = dataContract.CategoryName,
            DisplayScript = dataContract.DisplayScript,
            DisplayScriptHashRequired = true
        };
    }
}