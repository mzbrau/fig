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
            eventLog.NewValue = encryptionService.Encrypt(eventLog.NewValue);
            eventLog.NewValueEncrypted = true;
        }

        if (eventLog.OriginalValue != null)
        {
            eventLog.OriginalValue = encryptionService.Encrypt(eventLog.OriginalValue);
            eventLog.OriginalValueEncrypted = true;
        }
    }

    public static void Decrypt(this EventLogBusinessEntity eventLog,
        IEncryptionService encryptionService)
    {
        if (eventLog.NewValueEncrypted)
        {
            eventLog.NewValue = encryptionService.Decrypt(eventLog.NewValue!);
        }

        if (eventLog.OriginalValueEncrypted)
        {
            eventLog.OriginalValue = encryptionService.Decrypt(eventLog.OriginalValue!);
        }
    }
}