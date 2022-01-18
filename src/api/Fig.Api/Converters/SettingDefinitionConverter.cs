using Fig.Contracts;
using Fig.Contracts.SettingDefinitions;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class SettingDefinitionConverter : ISettingDefinitionConverter
{
    private readonly ISettingVerificationConverter _settingVerificationConverter;

    public SettingDefinitionConverter(ISettingVerificationConverter settingVerificationConverter)
    {
        _settingVerificationConverter = settingVerificationConverter;
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
        return new SettingDefinitionDataContract
        {
            Name = businessEntity.Name,
            Description = businessEntity.Description,
            IsSecret = businessEntity.IsSecret,
            Value = businessEntity.Value,
            DefaultValue = businessEntity.DefaultValue,
            ValueType = businessEntity.ValueType,
            ValidationType =
                Enum.Parse<ValidationType>(businessEntity.ValidationType ?? ValidationType.None.ToString()),
            ValidationRegex = businessEntity.ValidationRegex,
            ValidationExplanation = businessEntity.ValidationExplanation,
            ValidValues = businessEntity.ValidValues?.ToList(),
            Group = businessEntity.Group,
            DisplayOrder = businessEntity.DisplayOrder,
            Advanced = businessEntity.Advanced,
            StringFormat = businessEntity.StringFormat,
            EditorLineCount = businessEntity.EditorLineCount
        };
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
            StringFormat = dataContract.StringFormat,
            EditorLineCount = dataContract.EditorLineCount
        };
    }
}