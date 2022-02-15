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
        
        var jsonValue = (string)JsonConvert.SerializeObject(settingValue.Value);
        if (jsonValue.Length < encryptionService.InputLimit)
        {
            settingValue.ValueAsJson = encryptionService.Encrypt(jsonValue);
            settingValue.IsEncrypted = true;
        }
        else
        {
            settingValue.ValueAsJson = jsonValue;
        }
    }

    public static void DeserializeAndDecrypt(this SettingValueBusinessEntity settingValue,
        IEncryptionService encryptionService)
    {
        if (settingValue.ValueAsJson == null)
            return;

        if (settingValue.IsEncrypted)
        {
            settingValue.ValueAsJson = encryptionService.Decrypt(settingValue.ValueAsJson);
            settingValue.Value = JsonConvert.DeserializeObject(settingValue.ValueAsJson, settingValue.ValueType);
        }
        else
        {
            settingValue.Value = JsonConvert.DeserializeObject(settingValue.ValueAsJson, settingValue.ValueType);
        }
    }
}