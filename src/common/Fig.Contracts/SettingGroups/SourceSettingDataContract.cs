using System;

namespace Fig.Contracts.SettingGroups
{
    public class SourceSettingDataContract
    {
        public SourceSettingDataContract(string clientName, string settingName)
        {
            ClientName = clientName;
            SettingName = settingName;
        }

        public string ClientName { get; set; }

        public string SettingName { get; set; }
    }
}
