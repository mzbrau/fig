using Fig.Api.Services;
using Fig.Common.NetStandard.Json;
using Fig.Contracts.Scheduling;
using Fig.Datalayer.BusinessEntities;
using Newtonsoft.Json;
using Fig.Contracts.Settings;

namespace Fig.Api.ExtensionMethods;

public static class DeferredChangeBusinessEntityExtensions
{
    // Cache for JsonSerializerSettings to avoid creating them multiple times
    private static readonly JsonSerializerSettings SerializerSettings = JsonSettings.FigDefault;

    public static void SerializeAndEncrypt(this DeferredChangeBusinessEntity change,
        IEncryptionService encryptionService)
    {
        change.ChangeSetAsJson = SerializeAndEncryptValue(change.ChangeSet, encryptionService);
    }

    public static void DeserializeAndDecrypt(this DeferredChangeBusinessEntity change,
        IEncryptionService encryptionService)
    {
        change.ChangeSet = DeserializeAndDecryptValue(change.ChangeSetAsJson, encryptionService);
    }

    public static DeferredChangeDataContract Convert(this DeferredChangeBusinessEntity change)
    {
        return new DeferredChangeDataContract(change.Id!.Value,
            change.ExecuteAtUtc,
            change.RequestingUser,
            change.ClientName,
            change.Instance,
            change.ChangeSet?.Clone().Redact());
    }

    private static string? SerializeAndEncryptValue(SettingValueUpdatesDataContract? value, IEncryptionService encryptionService)
    {
        if (value == null)
            return null;
        
        var jsonValue = JsonConvert.SerializeObject(value, SerializerSettings);

        return encryptionService.Encrypt(jsonValue);
    }

    private static SettingValueUpdatesDataContract? DeserializeAndDecryptValue(string? value,
        IEncryptionService encryptionService)
    {
        if (value == null)
            return null;

        value = encryptionService.Decrypt(value);
        return value is null ? null : JsonConvert.DeserializeObject(value, SerializerSettings) as SettingValueUpdatesDataContract;
    }
}