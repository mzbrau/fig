using Fig.Api.Comparers;
using Fig.Api.Exceptions;
using Fig.Api.Services;
using Fig.Api.SettingVerification.Dynamic;
using Fig.Datalayer.BusinessEntities;
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

    public static SettingVerificationBase? GetVerification(this SettingClientBusinessEntity client, string name)
    {
        var pluginVerification = client.PluginVerifications.FirstOrDefault(a => a.Name == name);
        if (pluginVerification != null)
            return pluginVerification;

        var dynamicVerification = client.DynamicVerifications.FirstOrDefault(a => a.Name == name);
        return dynamicVerification;
    }

    public static void SerializeAndEncrypt(this SettingClientBusinessEntity client,
        IEncryptionService encryptionService,
        ICodeHasher codeHasher)
    {
        foreach (var setting in client.Settings)
        {
            ValueTuple<string?, bool> valueOutput = SerializeAndEncryptValue(setting.Value, setting.IsSecret, encryptionService);
            setting.ValueAsJson = valueOutput.Item1;
            setting.IsEncrypted = valueOutput.Item2;
        }

        foreach (var verification in client.DynamicVerifications)
        {
            verification.CodeHash = codeHasher.GetHash(verification.Code);
        }
    }

    public static void DeserializeAndDecrypt(this SettingClientBusinessEntity client,
        IEncryptionService encryptionService)
    {
        foreach (var setting in client.Settings)
        {
            setting.Value = DeserializeAndDecryptValue(setting.ValueAsJson,
                setting.IsEncrypted,
                setting.ValueType,
                encryptionService);
        }
    }

    public static string GetIdentifier(this SettingClientBusinessEntity client)
    {
        return $"{client.Name}-{client.Instance}";
    }

    private static (string?, bool) SerializeAndEncryptValue(dynamic? value, bool isSecret, IEncryptionService encryptionService)
    {
        if (value == null)
            return (null, false);

        var jsonValue = (string)JsonConvert.SerializeObject(value);
        if (jsonValue.Length > encryptionService.InputLimit)
        {
            if (isSecret)
                throw new InvalidSettingException(
                    $"Secret setting has value longer than {encryptionService.InputLimit} which is the maximum that can be encrypted.");
            return (jsonValue, false);
        }

        return (encryptionService.Encrypt(jsonValue), true);
    }

    private static object? DeserializeAndDecryptValue(string? value, bool isEncrypted, Type? type,
        IEncryptionService encryptionService)
    {
        if (value == null || type == null)
            return default;

        if (isEncrypted)
        {
            value = encryptionService.Decrypt(value);
        }
        
        return JsonConvert.DeserializeObject(value, type);
    }
}