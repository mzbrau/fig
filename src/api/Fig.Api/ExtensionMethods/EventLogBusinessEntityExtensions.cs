using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class EventLogBusinessEntityExtensions
{
    public static void Encrypt(this EventLogBusinessEntity eventLog,
        IEncryptionService encryptionService)
    {
        if (eventLog.NewValue != null)
            eventLog.NewValueEncrypted = encryptionService.Encrypt(eventLog.NewValue);

        if (eventLog.OriginalValue != null)
            eventLog.OriginalValueEncrypted = encryptionService.Encrypt(eventLog.OriginalValue);
    }

    public static void Decrypt(this EventLogBusinessEntity eventLog,
        IEncryptionService encryptionService, bool tryFallbackFirst = false)
    {
        eventLog.NewValue = encryptionService.Decrypt(eventLog.NewValueEncrypted, tryFallbackFirst);
        eventLog.OriginalValue = encryptionService.Decrypt(eventLog.OriginalValueEncrypted, tryFallbackFirst);
    }
}