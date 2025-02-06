using Fig.Api.Comparers;
using Fig.Api.Services;
using Fig.Api.Validators;
using Fig.Common.NetStandard.Json;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

namespace Fig.Api.ExtensionMethods;

public static class SettingsClientBusinessEntityExtensions
{
    public static SettingClientBusinessEntity CreateOverride(this SettingClientBusinessEntity original,
        string? instance)
    {
        return new SettingClientBusinessEntity
        {
            Name = original.Name,
            Description = original.Description,
            ClientSecret = original.ClientSecret,
            Instance = instance,
            Settings = original.Settings.Select(a => a.Clone()).ToList()
        };
    }

    public static bool HasEquivalentDefinitionTo(this SettingClientBusinessEntity original,
        SettingClientBusinessEntity other)
    {
        return new ClientComparer().Equals(original, other);
    }

    public static SettingVerificationBusinessEntity? GetVerification(this SettingClientBusinessEntity client, string name)
    {
        return client.Verifications.FirstOrDefault(a => a.Name == name);
    }

    public static void SerializeAndEncrypt(this SettingClientBusinessEntity client,
        IEncryptionService encryptionService)
    {
        foreach (var setting in client.Settings)
        {
            setting.ValueAsJson = SerializeAndEncryptValue(setting.Value, encryptionService);
        }
    }

    public static void DeserializeAndDecrypt(this SettingClientBusinessEntity client,
        IEncryptionService encryptionService)
    {
        foreach (var setting in client.Settings)
        {
            setting.Value = DeserializeAndDecryptValue(setting.ValueAsJson, encryptionService);
        }
    }

    public static string GetIdentifier(this SettingClientBusinessEntity client)
    {
        return $"{client.Name}-{client.Instance}";
    }

    public static bool IsInSecretChangePeriod(this ClientBase client)
    {
        if (client.PreviousClientSecretExpiryUtc == null)
            return false;
        
        return client.PreviousClientSecretExpiryUtc > DateTime.UtcNow;
    }

    public static void HashCode(this SettingClientBusinessEntity client, ICodeHasher codeHasher)
    {
        foreach (var setting in client.Settings)
        {
            if (setting.DisplayScriptHashRequired)
            {
                setting.DisplayScriptHash = string.IsNullOrWhiteSpace(setting.DisplayScript)
                    ? null
                    : codeHasher.GetHash(setting.DisplayScript);
            }
        }
    }

    public static void ValidateCodeHash(this SettingClientBusinessEntity client, ICodeHasher codeHasher, ILogger logger)
    {
        foreach (var setting in client.Settings)
        {
            if (string.IsNullOrWhiteSpace(setting.DisplayScriptHash))
            {
                setting.DisplayScript = null;
            }
            else if (!string.IsNullOrWhiteSpace(setting.DisplayScript))
            {
                if (!codeHasher.IsValid(setting.DisplayScriptHash, setting.DisplayScript))
                {
                    setting.DisplayScript = null;
                    logger.LogWarning("Invalid code hash for display script for setting {SettingName} in client {ClientName}. Script has been removed", setting.Name, client.Name);
                }
            }
        }
    }

    private static string? SerializeAndEncryptValue(SettingValueBaseBusinessEntity? value, IEncryptionService encryptionService)
    {
        if (value == null)
            return null;
        
        var jsonValue = JsonConvert.SerializeObject(value, JsonSettings.FigDefault);

        return encryptionService.Encrypt(jsonValue);
    }

    private static SettingValueBaseBusinessEntity? DeserializeAndDecryptValue(string? value,
        IEncryptionService encryptionService)
    {
        if (value == null)
            return default;

        value = encryptionService.Decrypt(value);
        return value is null ? null : JsonConvert.DeserializeObject(value, JsonSettings.FigDefault) as SettingValueBaseBusinessEntity;
    }
}