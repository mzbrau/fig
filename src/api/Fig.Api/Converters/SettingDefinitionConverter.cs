using Fig.Api.BusinessEntities;
using Fig.Api.Controllers;
using Fig.Contracts;
using Fig.Contracts.SettingConfiguration;
using Fig.Contracts.SettingDefinitions;

namespace Fig.Api.Converters;

public class SettingDefinitionConverter : ISettingDefinitionConverter
{
    public SettingsClientBusinessEntity Convert(SettingsClientDefinitionDataContract dataContract)
    {
        return new SettingsClientBusinessEntity
        {
            Name = dataContract.Name,
            ClientSecret = dataContract.ClientSecret,
            Settings = dataContract.Settings.Select(Convert).ToList()
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