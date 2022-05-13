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
            var result = encryptionService.Encrypt(jsonValue);
            settingValue.ValueAsJson = result.EncryptedValue;
            settingValue.EncryptionCertificateThumbprint = result.Thumbprint;
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

        if (!string.IsNullOrEmpty(settingValue.EncryptionCertificateThumbprint))
        {
            settingValue.ValueAsJson = encryptionService.Decrypt(settingValue.ValueAsJson, settingValue.EncryptionCertificateThumbprint);
            settingValue.Value = JsonConvert.DeserializeObject(settingValue.ValueAsJson, settingValue.ValueType);
        }
        else
        {
            settingValue.Value = JsonConvert.DeserializeObject(settingValue.ValueAsJson, settingValue.ValueType);
        }
    }
}