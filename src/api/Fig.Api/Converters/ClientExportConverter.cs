using Fig.Api.Encryption;
using Fig.Api.Services;
using Fig.Contracts.ImportExport;
using Fig.Datalayer.BusinessEntities;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
        return new SettingClientExportDataContract
        {
            Name = client.Name,
            ClientSecret = client.ClientSecret,
            Instance = client.Instance,
            Settings = client.Settings.Select(s => Convert(s, decryptSecrets)).ToList(),
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
            Value = string.IsNullOrEmpty(setting.EncryptionCertificateThumbprint) || setting.Value == null ? setting.Value : GetDecryptedValue(setting.Value, setting.EncryptionCertificateThumbprint, setting.ValueType),
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

    private SettingExportDataContract Convert(SettingBusinessEntity setting, bool decryptSecrets)
    {
        string? thumbprint = null;
        dynamic? value = setting.Value;
        if (!decryptSecrets && setting.IsSecret && setting.Value != null)
        {
            ValueTuple<string, string> result = GetEncryptedValue(setting.Value);
            value = result.Item1;
            thumbprint = result.Item2;
        }

        return new SettingExportDataContract
        {
            Name = setting.Name,
            Description = setting.Description,
            IsSecret = setting.IsSecret,
            ValueType = setting.ValueType,
            Value = value,
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
            DataGridDefinitionJson = setting.DataGridDefinitionJson,
            EncryptionCertificateThumbprint = thumbprint
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

    private dynamic GetDecryptedValue(string settingValue, string thumbprint, Type type)
    {
        var value = _encryptionService.Decrypt(settingValue, thumbprint);
        return type == typeof(string) ? value : JsonConvert.DeserializeObject(value, type);
    }

    private (string EncryptedValue, string Thumbprint) GetEncryptedValue(dynamic settingValue)
    {
        EncryptionResultModel result = _encryptionService.Encrypt(settingValue);
        return (result.EncryptedValue, result.Thumbprint);
    }
}