using Fig.Api.Utils;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Secrets;

public interface ISecretStoreHandler
{
    Task SaveSecrets(SettingClientBusinessEntity client);

    Task SaveSecrets(SettingClientBusinessEntity client, List<ChangedSetting> changes);
    
    Task HydrateSecrets(SettingClientBusinessEntity client);
    
    void ClearSecrets(SettingClientBusinessEntity client);
}