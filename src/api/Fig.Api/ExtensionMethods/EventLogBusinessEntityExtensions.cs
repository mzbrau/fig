using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class EventLogBusinessEntityExtensions
{
    public static void Encrypt(this EventLogBusinessEntity eventLog,
        IEncryptionService encryptionService)
    {
        if (eventLog.NewValue != null)
        {
            var result = encryptionService.Encrypt(eventLog.NewValue);
            eventLog.NewValue = result.EncryptedValue;
            eventLog.NewValueEncryptionThumbprint = result.Thumbprint;
        }

        if (eventLog.OriginalValue != null)
        {
            var result = encryptionService.Encrypt(eventLog.OriginalValue);
            eventLog.OriginalValue = result.EncryptedValue;
            eventLog.OriginalValueEncryptionThumbprint = result.Thumbprint;
        }
    }

    public static void Decrypt(this EventLogBusinessEntity eventLog,
        IEncryptionService encryptionService)
    {
        if (!string.IsNullOrEmpty(eventLog.NewValueEncryptionThumbprint))
        {
            eventLog.NewValue = encryptionService.Decrypt(eventLog.NewValue!, eventLog.NewValueEncryptionThumbprint);
        }

        if (!string.IsNullOrEmpty(eventLog.OriginalValueEncryptionThumbprint))
        {
            eventLog.OriginalValue = encryptionService.Decrypt(eventLog.OriginalValue!, eventLog.OriginalValueEncryptionThumbprint);
        }
    }
}