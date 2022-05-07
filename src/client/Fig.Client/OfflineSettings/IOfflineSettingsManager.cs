using System.Collections.Generic;
using Fig.Contracts.Settings;

namespace Fig.Client.OfflineSettings
{
    public interface IOfflineSettingsManager
    {
        void Save(string clientName, IEnumerable<SettingDataContract> settings);

        IEnumerable<SettingDataContract> Get(string clientName);

        void Delete(string clientName);
    }
}