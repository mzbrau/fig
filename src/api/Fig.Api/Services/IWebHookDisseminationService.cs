using Fig.Api.Utils;
using Fig.Contracts.Status;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Services;

public interface IWebHookDisseminationService
{
    Task NewClientRegistration(SettingClientBusinessEntity client);

    Task UpdatedClientRegistration(SettingClientBusinessEntity client);

    Task SettingValueChanged(List<ChangedSetting> changes, SettingClientBusinessEntity client, string? username);
    
    Task MemoryLeakDetected(ClientStatusBusinessEntity client, ClientRunSessionBusinessEntity session);
    
    Task ClientConnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client);
    
    Task ClientDisconnected(ClientRunSessionBusinessEntity session, ClientStatusBusinessEntity client);
    
    Task ConfigurationErrorStatusChanged(ClientStatusBusinessEntity client, StatusRequestDataContract statusRequest);
}