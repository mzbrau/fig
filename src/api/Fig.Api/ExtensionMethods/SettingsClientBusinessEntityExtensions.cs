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
    // Cache for JsonSerializerSettings to avoid creating them multiple times
    private static readonly JsonSerializerSettings SerializerSettings = JsonSettings.FigDefault;

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

    public static void SerializeAndEncrypt(this SettingClientBusinessEntity client,
        IEncryptionService encryptionService)
    {
        foreach (var setting in client.Settings)
        {
            setting.ValueAsJson = SerializeAndEncryptValue(setting.Value, encryptionService);
        }
    }

    public static void DeserializeAndDecrypt(this SettingClientBusinessEntity client,
        IEncryptionService encryptionService,
        bool tryFallbackFirst = false)
    {
        foreach (var setting in client.Settings)
        {
            setting.Value = DeserializeAndDecryptValue(setting.ValueAsJson, encryptionService, tryFallbackFirst);
        }
    }

    public static void DeserializeAndDecryptBestEffort(this SettingClientBusinessEntity client,
        IEncryptionService encryptionService,
        Action<SettingBusinessEntity, Exception> recordFailure,
        bool tryFallbackFirst = false)
    {
        foreach (var setting in client.Settings.ToList())
        {
            try
            {
                setting.Value = DeserializeAndDecryptValue(setting.ValueAsJson, encryptionService, tryFallbackFirst);
            }
            catch (Exception ex) when (ex is JsonException or System.Security.Cryptography.CryptographicException)
            {
                recordFailure(setting, ex);
                client.Settings.Remove(setting);
            }
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
                if (!codeHasher.IsValid(client.Name, setting.Name, setting.DisplayScriptHash, setting.DisplayScript))
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

        var jsonValue = JsonConvert.SerializeObject(value, SerializerSettings);

        return encryptionService.Encrypt(jsonValue);
    }

    private static SettingValueBaseBusinessEntity? DeserializeAndDecryptValue(string? value,
        IEncryptionService encryptionService,
        bool tryFallbackFirst)
    {
        if (value == null)
            return default;

        value = encryptionService.DecryptWithValidation(value, IsValidSettingValueJson, tryFallbackFirst);
        return value is null
            ? null
            : JsonConvert.DeserializeObject<SettingValueBaseBusinessEntity>(value, SerializerSettings)
              ?? throw new JsonSerializationException("Decrypted setting value JSON did not contain a setting value.");
    }

    private static bool IsValidSettingValueJson(string value)
    {
        try
        {
            return JsonConvert.DeserializeObject<SettingValueBaseBusinessEntity>(value, SerializerSettings) is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}