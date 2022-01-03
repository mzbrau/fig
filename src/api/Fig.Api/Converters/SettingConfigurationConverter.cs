using Fig.Api.BusinessEntities;
using Fig.Contracts.SettingConfiguration;

namespace Fig.Api.Converters;

public class SettingConfigurationConverter : ISettingConfigurationConverter
{
    public SettingsClientConfigurationDataContract Convert(SettingsClientBusinessEntity businessEntity)
    {
        return new SettingsClientConfigurationDataContract
        {
            ServiceName = businessEntity.Name,
            Hostname = businessEntity.Qualifiers.Hostname,
            Username = businessEntity.Qualifiers.Username,
            Instance = businessEntity.Qualifiers.Instance,
            Settings = businessEntity.Settings.Select(Convert).ToList(),
        };
    }
    
    private SettingConfigurationDataContract Convert(SettingBusinessEntity businessEntity)
    {
        return new SettingConfigurationDataContract
        {
            Name = businessEntity.Name,
            FriendlyName = businessEntity.FriendlyName,
            Description = businessEntity.Description,
            IsSecret = businessEntity.IsSecret,
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
}