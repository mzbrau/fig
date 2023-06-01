using Fig.Contracts.WebHook;

namespace Fig.Api.ExtensionMethods;

public static class WebHookTypeExtensions
{
    public static string GetRoute(this WebHookType webHookType)
    {
        return webHookType switch
        {
            WebHookType.NewClientRegistration => "NewClientRegistration",
            WebHookType.UpdatedClientRegistration => "UpdatedClientRegistration",
            WebHookType.ClientStatusChanged => "ClientStatusChanged",
            WebHookType.MemoryLeakDetected => "MemoryLeakDetected",
            WebHookType.SettingValueChanged => "SettingValueChanged",
            WebHookType.MinRunSessions => "BelowMinRunSessions",
            _ => throw new ArgumentOutOfRangeException(nameof(webHookType), webHookType, "Unknown web hook type")
        };
    }
}