using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.ExtensionMethods;

public static class CheckpointDataBusinessEntityExtensions
{
    public static void Encrypt(this CheckPointDataBusinessEntity data,
        IEncryptionService encryptionService)
    {
        if (data.ExportAsJson != null)
            data.ExportAsJson = encryptionService.Encrypt(data.ExportAsJson);
    }

    public static void Decrypt(this CheckPointDataBusinessEntity data,
        IEncryptionService encryptionService, bool tryFallbackFirst = false)
    {
        data.ExportAsJson = encryptionService.Decrypt(data.ExportAsJson, tryFallbackFirst, false);
    }
}