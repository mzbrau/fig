using Fig.Api.BusinessEntities;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.Converters;

public class SettingDefinitionConverter : ISettingDefinitionConverter
{
    public SettingsClientBusinessEntity Convert(SettingsClientDefinitionDataContract dataContract)
    {
        return new SettingsClientBusinessEntity
        {
            Name = dataContract.Name,
            Settings = dataContract.Settings.Select(Convert).ToList()
        };
    }

    public SettingsClientDefinitionDataContract Convert(SettingsClientBusinessEntity businessEntity)
    {
        return new SettingsClientDefinitionDataContract
        {
            Id = businessEntity.Id,
            Name = businessEntity.Name,
            Instance = businessEntity.Instance,
            Settings = businessEntity.Settings.Select(Convert).ToList()
        };
    }

    private SettingDefinitionDataContract Convert(SettingBusinessEntity businessEntity)
    {
        return new SettingDefinitionDataContract
        {
            Name = businessEntity.Name,
            FriendlyName = businessEntity.FriendlyName,
            Description = businessEntity.FriendlyName,
            IsSecret = businessEntity.IsSecret,
            Value = businessEntity.Value,
            DefaultValue = businessEntity.DefaultValue,
            ValidationType = businessEntity.ValidationType,
            ValidationRegex = businessEntity.ValidationRegex,
            ValidationExplanation = businessEntity.ValidationExplanation,
            ValidValues = businessEntity.ValidValues,
            Group = businessEntity.Group,
            DisplayOrder = businessEntity.DisplayOrder,
            Advanced = businessEntity.Advanced,
            StringFormat = businessEntity.StringFormat
        };
    }


    private SettingBusinessEntity Convert(SettingDefinitionDataContract dataContract)
    {
        return new SettingBusinessEntity
        {
            Name = dataContract.Name,
            FriendlyName = dataContract.FriendlyName,
            Description = dataContract.Description,
            IsSecret = dataContract.IsSecret,
            Value = dataContract.DefaultValue,
            DefaultValue = dataContract.DefaultValue,
            ValidationType = dataContract.ValidationType,
            ValidationRegex = dataContract.ValidationRegex,
            ValidationExplanation = dataContract.ValidationExplanation,
            ValidValues = dataContract.ValidValues,
            Group = dataContract.Group,
            DisplayOrder = dataContract.DisplayOrder,
            Advanced = dataContract.Advanced,
            StringFormat = dataContract.StringFormat
        };
    }
}