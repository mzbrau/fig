using Fig.Api.Services;
using Fig.Common;
using Fig.Contracts;
using Fig.Contracts.ImportExport;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;

namespace Fig.Api.Converters;

public class ClientExportConverter : IClientExportConverter
{
    private readonly IEncryptionService _encryptionService;
    private readonly ISettingConverter _settingConverter;

    public ClientExportConverter(IEncryptionService encryptionService, ISettingConverter settingConverter)
    {
        _encryptionService = encryptionService;
        _settingConverter = settingConverter;
    }

    public SettingClientExportDataContract Convert(SettingClientBusinessEntity client, bool decryptSecrets)
    {
        return new SettingClientExportDataContract(client.Name,
            client.Description,
            client.ClientSecret,
            client.Instance,
            client.Settings.Select(s => Convert(s, decryptSecrets)).ToList(),
            client.PluginVerifications.Select(Convert).ToList(),
            client.DynamicVerifications.Select(Convert).ToList());
    }

    public SettingClientValueExportDataContract ConvertValueOnly(SettingClientBusinessEntity client)
    {
        return new SettingClientValueExportDataContract(
            client.Name,
            client.Instance,
            client.Settings.Select(ConvertValueOnlySetting).ToList());
    }

    private SettingValueExportDataContract ConvertValueOnlySetting(SettingBusinessEntity setting)
    {
        return new SettingValueExportDataContract(setting.Name, setting.Value?.GetValue());
    }

    public SettingClientBusinessEntity Convert(SettingClientExportDataContract client)
    {
        return new SettingClientBusinessEntity
        {
            Name = client.Name,
            Description = client.Description,
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
            Value = setting is { IsEncrypted: true, Value: StringSettingDataContract strValue }
                ? _settingConverter.Convert(GetDecryptedValue(strValue, setting.ValueType))
                : _settingConverter.Convert(setting.Value),
            DefaultValue = _settingConverter.Convert(setting.DefaultValue),
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
            EnablesSettings = setting.EnablesSettings,
            SupportsLiveUpdate = setting.SupportsLiveUpdate,
            LastChanged = setting.LastChanged
        };
    }

    private SettingExportDataContract Convert(SettingBusinessEntity setting, bool decryptSecrets)
    {
        var value = _settingConverter.Convert(setting.Value);
        var isEncrypted = false;
        if (!decryptSecrets && setting is { IsSecret: true, Value: not null })
        {
            value = new StringSettingDataContract(GetEncryptedValue(setting.Value));
            isEncrypted = true;
        }

        return new SettingExportDataContract(
            setting.Name,
            setting.Description,
            setting.IsSecret,
            setting.ValueType,
            value,
            _settingConverter.Convert(setting.DefaultValue),
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
            setting.EnablesSettings,
            setting.SupportsLiveUpdate,
            setting.LastChanged);
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

    private SettingValueBaseDataContract? GetDecryptedValue(StringSettingDataContract settingValue, Type type)
    {
        var value = _encryptionService.Decrypt(settingValue.Value);
        return value is null ? null : ValueDataContractFactory.CreateContract(settingValue.Value, type);
    }

    private string? GetEncryptedValue(SettingValueBaseBusinessEntity settingValue)
    {
        return _encryptionService.Encrypt(settingValue.GetValue()?.ToString());
    }
}