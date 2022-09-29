using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;

namespace Fig.Api.ExtensionMethods;

public static class SettingValueBusinessEntityExtensions
{
    public static void SerializeAndEncrypt(this SettingValueBusinessEntity settingValue,
        IEncryptionService encryptionService)
    {
        if (settingValue.Value == null)
            return;

        var jsonValue = (string) JsonConvert.SerializeObject(settingValue.Value);
        settingValue.ValueAsJson = encryptionService.Encrypt(jsonValue);
    }

    public static void DeserializeAndDecrypt(this SettingValueBusinessEntity settingValue,
        IEncryptionService encryptionService)
    {
        if (settingValue.ValueAsJson == null)
            return;

        settingValue.ValueAsJson = encryptionService.Decrypt(settingValue.ValueAsJson);
        if (settingValue.ValueAsJson == null)
            return;
        
        settingValue.Value = JsonConvert.DeserializeObject(settingValue.ValueAsJson, settingValue.ValueType);
    }
    
    public static SettingValueBusinessEntity Clone(this SettingValueBusinessEntity original, Guid clientId)
    {
        return new SettingValueBusinessEntity
        {
            ClientId = clientId,
            SettingName = original.SettingName,
            ValueType = original.ValueType,
            Value = original.Value,
            ValueAsJson = original.ValueAsJson,
            ChangedAt = original.ChangedAt,
            ChangedBy = original.ChangedBy
        };
    }
}