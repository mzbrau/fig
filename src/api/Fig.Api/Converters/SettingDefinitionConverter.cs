using System.Diagnostics;
using System.Globalization;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Contracts.Authentication;
using Fig.Contracts.CustomActions;
using Fig.Contracts.SettingDefinitions;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;
using NHibernate;

namespace Fig.Api.Converters;

public class SettingDefinitionConverter : ISettingDefinitionConverter
{
    private readonly IEncryptionService _encryptionService;
    private readonly IValidValuesHandler _validValuesHandler;
    private readonly ISettingConverter _settingConverter;

    public SettingDefinitionConverter(
        IEncryptionService encryptionService, IValidValuesHandler validValuesHandler, ISettingConverter settingConverter)
    {
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
            Settings = dataContract.Settings.Select(Convert).ToList()
        };
    }

    public async Task<SettingsClientDefinitionDataContract> Convert(SettingClientBusinessEntity businessEntity,
        bool allowDisplayScripts, UserDataContract? authenticatedUser)
    {
        var settings = await Task.WhenAll(
            businessEntity.Settings
                .Where(authenticatedUser.HasPermissionForClassification).
                Select(s => Convert(s, allowDisplayScripts)));
        var contract = new SettingsClientDefinitionDataContract(businessEntity.Name,
            string.Empty, // Description will the loaded later.
            businessEntity.Instance,
            businessEntity.Settings.Any(a => !string.IsNullOrEmpty(a.DisplayScript)),
            settings.ToList(),
            new List<SettingDataContract>(),
            businessEntity.CustomActions.Select(Convert).ToList());
        
        var isDescriptionInitialized = NHibernateUtil.IsPropertyInitialized(businessEntity, nameof(businessEntity.Description));
        Debug.Assert(isDescriptionInitialized == false, "Description property not initialized");
        return contract;
    }

    private CustomActionDefinitionDataContract Convert(CustomActionBusinessEntity customAction)
    {
        return new CustomActionDefinitionDataContract(customAction.Name,
            customAction.ButtonName,
            customAction.Description,
            customAction.SettingsUsed);
    }

    private async Task<SettingDefinitionDataContract> Convert(SettingBusinessEntity businessEntity, bool allowDisplayScripts)
    {
        var dataGridDefinition = businessEntity.GetDataGridDefinition();
        List<string>? validValues = null;
        if (dataGridDefinition is null)
        {
            validValues = await _validValuesHandler.GetValidValues(businessEntity.ValidValues,
                businessEntity.LookupTableKey, businessEntity.ValueType, businessEntity.Value);
        }
        else if (dataGridDefinition.Columns.Count == 1)
        {
            var firstColumn = dataGridDefinition.Columns.First();
            validValues = firstColumn.ValidValues = await _validValuesHandler.GetValidValues(firstColumn.ValidValues,
                businessEntity.LookupTableKey, firstColumn.ValueType, businessEntity.Value);
            firstColumn.ValidValues = validValues;
        }

        SettingValueBaseDataContract? defaultValue;
        if (validValues is null && dataGridDefinition is null)
        {
            defaultValue = _settingConverter.Convert(businessEntity.DefaultValue, businessEntity.HasSchema(),
                dataGridDefinition);
        }
        else if (dataGridDefinition is null)
        {
            defaultValue = new StringSettingDataContract(System.Convert.ToString(businessEntity.DefaultValue?.GetValue(), CultureInfo.InvariantCulture));
        }
        else
        {
            defaultValue = _settingConverter.Convert(businessEntity.DefaultValue, businessEntity.HasSchema(), dataGridDefinition);
        }

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
            allowDisplayScripts ? businessEntity.DisplayScript : null,
            businessEntity.IsExternallyManaged,
            businessEntity.Classification,
            businessEntity.EnvironmentSpecific,
            businessEntity.LookupKeySettingName,
            businessEntity.Indent,
            businessEntity.DependsOnProperty,
            businessEntity.DependsOnValidValues);
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
            ? _validValuesHandler.GetValueFromValidValues(setting.Value?.GetValue(), validValues, dataGridDefinition, setting.LookupKeySettingName)
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
            DisplayScriptHashRequired = true,
            IsExternallyManaged = dataContract.IsExternallyManaged,
            Classification = dataContract.Classification,
            EnvironmentSpecific = dataContract.EnvironmentSpecific,
            LookupKeySettingName = dataContract.LookupKeySettingName,
            Indent = dataContract.Indent,
            DependsOnProperty = dataContract.DependsOnProperty,
            DependsOnValidValues = dataContract.DependsOnValidValues
        };
    }
}