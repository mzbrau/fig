using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class WebHookClientBusinessEntityExtensions
{
    public static void Encrypt(this WebHookClientBusinessEntity client,
        IEncryptionService encryptionService)
    {
        client.SecretEncrypted = encryptionService.Encrypt(client.Secret)!;
    }

    public static void Decrypt(this WebHookClientBusinessEntity client,
        IEncryptionService encryptionService)
    {
        client.Secret = encryptionService.Decrypt(client.SecretEncrypted)!;
    }
}