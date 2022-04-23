using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Converters;

public class ClientExportConverter : IClientExportConverter
{
    public SettingClientExportDataContract Convert(SettingClientBusinessEntity client)
    {
        return new SettingClientExportDataContract
        {
            Name = client.Name,
            ClientSecret = client.ClientSecret,
            Instance = client.Instance,
            Settings = client.Settings.Select(Convert).ToList(),
            PluginVerifications = client.PluginVerifications.Select(Convert).ToList(),
            DynamicVerifications = client.DynamicVerifications.Select(Convert).ToList()
        };
    }

    public SettingClientBusinessEntity Convert(SettingClientExportDataContract client)
    {
        return new SettingClientBusinessEntity
        {
            Name = client.Name,
            ClientSecret = client.ClientSecret,
            Instance = client.Instance,
            LastRegistration = DateTime.UtcNow,
            Settings = client.Settings.Select(Convert).ToList(),
            PluginVerifications = client.PluginVerifications?.Select(Convert).ToList() ?? new List<SettingPluginVerificationBusinessEntity>(),
            DynamicVerifications = client.DynamicVerifications?.Select(Convert).ToList() ?? new List<SettingDynamicVerificationBusinessEntity>()
        };
    }

    private SettingPluginVerificationBusinessEntity Convert(PluginVerificationExportDataContract verification)
    {
        return new SettingPluginVerificationBusinessEntity
        {
            Name = verification.Name,
            PropertyArguments = verification.PropertyArguments,
        };
    }

    private SettingDynamicVerificationBusinessEntity Convert(DynamicVerificationExportDataContract verification)
    {
        return new SettingDynamicVerificationBusinessEntity
        {
            Name = verification.Name,
            Description = verification.Description,
            Code = verification.Code,
            TargetRuntime = verification.TargetRuntime,
            SettingsVerified = verification.SettingsVerified,
        };
    }

    private SettingBusinessEntity Convert(SettingExportDataContract setting)
    {
        return new SettingBusinessEntity
        {
            Name = setting.Name,
            Description = setting.Description,
            IsSecret = setting.IsSecret,
            ValueType = setting.ValueType,
            Value = setting.Value,
            DefaultValue = setting.DefaultValue,
            JsonSchema = setting.JsonSchema,
            ValidationType = setting.ValidationType,
            ValidationRegex = setting.ValidationRegex,
            ValidationExplanation = setting.ValidationExplanation,
            ValidValues = setting.ValidValues,
            Group = setting.Group,
            DisplayOrder = setting.DisplayOrder,
            Advanced = setting.Advanced,
            StringFormat = setting.StringFormat,
            EditorLineCount = setting.EditorLineCount,
            DataGridDefinitionJson = setting.DataGridDefinitionJson
        };
    }

    private SettingExportDataContract Convert(SettingBusinessEntity client)
    {
        return new SettingExportDataContract
        {
            Name = client.Name,
            Description = client.Description,
            IsSecret = client.IsSecret,
            ValueType = client.ValueType,
            Value = client.Value,
            DefaultValue = client.DefaultValue,
            JsonSchema = client.JsonSchema,
            ValidationType = client.ValidationType,
            ValidationRegex = client.ValidationRegex,
            ValidationExplanation = client.ValidationExplanation,
            ValidValues = client.ValidValues,
            Group = client.Group,
            DisplayOrder = client.DisplayOrder,
            Advanced = client.Advanced,
            StringFormat = client.StringFormat,
            EditorLineCount = client.EditorLineCount,
            DataGridDefinitionJson = null
        };
    }

    private PluginVerificationExportDataContract Convert(SettingPluginVerificationBusinessEntity verification)
    {
        return new PluginVerificationExportDataContract
        {
            Name = verification.Name,
            PropertyArguments = verification.PropertyArguments
        };
    }

    private DynamicVerificationExportDataContract Convert(SettingDynamicVerificationBusinessEntity verification)
    {
        return new DynamicVerificationExportDataContract
        {
            Name = verification.Name,
            Description = verification.Description,
            Code = verification.Code,
            TargetRuntime = verification.TargetRuntime,
            SettingsVerified = verification.SettingsVerified
        };
    }
}