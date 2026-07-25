using Fig.Api.Services;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Assistant;

public static class FigAssistantAvailability
{
    public static bool IsReady(FigConfigurationBusinessEntity configuration, IEncryptionService encryptionService)
    {
        if (!configuration.EnableFigAssistant)
            return false;

        if (string.IsNullOrWhiteSpace(configuration.FigAssistantEndpoint) ||
            string.IsNullOrWhiteSpace(configuration.FigAssistantModel) ||
            string.IsNullOrWhiteSpace(configuration.FigAssistantAccessTokenEncrypted))
        {
            return false;
        }

        var token = encryptionService.Decrypt(
            configuration.FigAssistantAccessTokenEncrypted,
            throwOnFailure: false);
        return !string.IsNullOrWhiteSpace(token) &&
               token != configuration.FigAssistantAccessTokenEncrypted;
    }
}
