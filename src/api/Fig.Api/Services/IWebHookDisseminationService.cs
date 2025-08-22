using Fig.Api.Utils;
using Fig.Api.WebHooks;
using Fig.Contracts.Health;
using Fig.Datalayer.BusinessEntities;
using Fig.WebHooks.Contracts;

namespace Fig.Api.Services;

public interface IWebHookDisseminationService
{
    Task NewClientRegistration(SettingClientBusinessEntity client);

    Task UpdatedClientRegistration(SettingClientBusinessEntity client);

    Task SettingValueChanged(List<ChangedSetting> changes, SettingClientBusinessEntity client, string? username, string changeMessage);

    Task ClientConnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client);
    
    Task ClientDisconnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client);

    Task HealthStatusChanged(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client,
        HealthDataContract healthDetails);
    
    Task SecurityEvent(SecurityEventWebHookData securityEvent);
}