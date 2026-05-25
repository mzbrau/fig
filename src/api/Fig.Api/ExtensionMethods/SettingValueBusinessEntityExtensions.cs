using Fig.Api.Services;
using Fig.Common.NetStandard.Json;
using Fig.Datalayer.BusinessEntities;
using Fig.Datalayer.BusinessEntities.SettingValues;
using Newtonsoft.Json;

namespace Fig.Api.ExtensionMethods;

public static class SettingValueBusinessEntityExtensions
{
    public static void SerializeAndEncrypt(this SettingValueBusinessEntity settingValue,
        IEncryptionService encryptionService)
    {
        settingValue.LastEncrypted = DateTime.UtcNow;
        if (settingValue.Value == null)
            return;

        var jsonValue = JsonConvert.SerializeObject(settingValue.Value, JsonSettings.FigDefault);
        settingValue.ValueAsJsonEncrypted = encryptionService.Encrypt(jsonValue);
    }

    public static void DeserializeAndDecrypt(this SettingValueBusinessEntity settingValue,
        IEncryptionService encryptionService, bool tryFallbackFirst = false)
    {
        if (settingValue.ValueAsJsonEncrypted == null)
            return;

        settingValue.ValueAsJson = encryptionService.DecryptWithValidation(settingValue.ValueAsJsonEncrypted,
            IsValidSettingValueJson,
            tryFallbackFirst);
        if (settingValue.ValueAsJson == null)
            return;

        settingValue.Value = JsonConvert.DeserializeObject<SettingValueBaseBusinessEntity>(settingValue.ValueAsJson,
                                 JsonSettings.FigDefault)
                             ?? throw new JsonSerializationException("Decrypted setting history value JSON did not contain a setting value.");
    }

    public static SettingValueBusinessEntity Clone(this SettingValueBusinessEntity original, Guid clientId)
    {
        return new SettingValueBusinessEntity
        {
            ClientId = clientId,
            SettingName = original.SettingName,
            Value = original.Value,
            ValueAsJson = original.ValueAsJson,
            ValueAsJsonEncrypted = original.ValueAsJsonEncrypted,
            ChangedAt = original.ChangedAt,
            ChangedBy = original.ChangedBy,
            ChangeMessage = original.ChangeMessage,
            LastEncrypted = original.LastEncrypted
        };
    }

    private static bool IsValidSettingValueJson(string value)
    {
        try
        {
            return JsonConvert.DeserializeObject<SettingValueBaseBusinessEntity>(value, JsonSettings.FigDefault) is not null;
        }
        catch (JsonException)
        {
            return false;
        }
    }
}