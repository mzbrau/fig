using Fig.Api.Services;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.Converters;

public class ClientExportConverter : IClientExportConverter
{
    private readonly IEncryptionService _encryptionService;

    public ClientExportConverter(IEncryptionService encryptionService)
    {
        _encryptionService = encryptionService;
    }

    public SettingClientExportDataContract Convert(SettingClientBusinessEntity client, bool decryptSecrets)
    {
        return new SettingClientExportDataContract(client.Name,
            client.ClientSecret,
            client.Instance,
            client.Settings.Select(s => Convert(s, decryptSecrets)).ToList(),
            client.PluginVerifications.Select(Convert).ToList(),
            client.DynamicVerifications.Select(Convert).ToList());
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
            PluginVerifications = client.PluginVerifications.Select(Convert).ToList(),
            DynamicVerifications = client.DynamicVerifications.Select(Convert).ToList()
        };
    }

    private SettingPluginVerificationBusinessEntity Convert(PluginVerificationExportDataContract verification)
    {
        return new SettingPluginVerificationBusinessEntity
        {
            Name = verification.Name,
            PropertyArguments = verification.PropertyArguments
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
            SettingsVerified = verification.SettingsVerified
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
            Value = !setting.IsEncrypted || setting.Value == null
                ? setting.Value
                : GetDecryptedValue(setting.Value, setting.ValueType),
            DefaultValue = setting.DefaultValue,
            JsonSchema = setting.JsonSchema,
            ValidationType = setting.ValidationType,
            ValidationRegex = setting.ValidationRegex,
            ValidationExplanation = setting.ValidationExplanation,
            ValidValues = setting.ValidValues,
            Group = setting.Group,
            DisplayOrder = setting.DisplayOrder,
            Advanced = setting.Advanced,
            LookupTableKey = setting.LookupTableKey,
            EditorLineCount = setting.EditorLineCount,
            DataGridDefinitionJson = setting.DataGridDefinitionJson,
            EnablesSettings = setting.EnablesSettings
        };
    }

    private SettingExportDataContract Convert(SettingBusinessEntity setting, bool decryptSecrets)
    {
        var value = setting.Value;
        var isEncrypted = false;
        if (!decryptSecrets && setting.IsSecret && setting.Value != null)
        {
            value = GetEncryptedValue(setting.Value);
            isEncrypted = true;
        }

        return new SettingExportDataContract(
            setting.Name,
            setting.Description,
            setting.IsSecret,
            setting.ValueType,
            value,
            setting.DefaultValue,
            isEncrypted,
            setting.JsonSchema,
            setting.ValidationType,
            setting.ValidationRegex,
            setting.ValidationExplanation,
            setting.ValidValues,
            setting.Group,
            setting.DisplayOrder,
            setting.Advanced,
            setting.LookupTableKey,
            setting.EditorLineCount,
            setting.DataGridDefinitionJson,
            setting.EnablesSettings);
    }

    private PluginVerificationExportDataContract Convert(SettingPluginVerificationBusinessEntity verification)
    {
        return new PluginVerificationExportDataContract(verification.Name, verification.PropertyArguments);
    }

    private DynamicVerificationExportDataContract Convert(SettingDynamicVerificationBusinessEntity verification)
    {
        return new DynamicVerificationExportDataContract(verification.Name, verification.Description, verification.Code,
            verification.TargetRuntime, verification.SettingsVerified);
    }

    private dynamic? GetDecryptedValue(string settingValue, Type type)
    {
        var value = _encryptionService.Decrypt(settingValue);
        if (value is null)
            return value;
        
        return type == typeof(string) ? value : JsonConvert.DeserializeObject(value, type);
    }

    private string GetEncryptedValue(dynamic settingValue)
    {
        return _encryptionService.Encrypt(settingValue);
    }
}