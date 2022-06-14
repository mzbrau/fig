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
        settingValue.Value = JsonConvert.DeserializeObject(settingValue.ValueAsJson, settingValue.ValueType);
    }
}