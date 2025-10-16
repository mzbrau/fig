using Fig.WebHooks.Contracts;

namespace Fig.Integration.MicrosoftSentinel.Handlers;

public interface IWebHookHandler
{
    Task<bool> HandleSecurityEventAsync(SecurityEventDataContract securityEvent);
    
    Task<bool> HandleSettingValueChangedAsync(SettingValueChangedDataContract settingValueChanged);
    
    Task<bool> HandleClientRegistrationAsync(ClientRegistrationDataContract clientRegistration);
    
    Task<bool> HandleClientStatusChangedAsync(ClientStatusChangedDataContract clientStatusChanged);
    
    Task<bool> HandleClientHealthChangedAsync(ClientHealthChangedDataContract clientHealthChanged);
    
    Task<bool> HandleMinRunSessionsAsync(MinRunSessionsDataContract minRunSessions);
}