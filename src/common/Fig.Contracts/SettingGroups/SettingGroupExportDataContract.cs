using System;
using System.Collections.Generic;

namespace Fig.Contracts.SettingGroups
{
    public class SettingGroupExportDataContract
    {
        public SettingGroupExportDataContract(DateTime exportedAt, int version, List<SettingGroupDataContract> groups)
        {
            ExportedAt = exportedAt;
            Version = version;
            Groups = groups;
        }

        public DateTime ExportedAt { get; set; }

        public int Version { get; set; }

        public List<SettingGroupDataContract> Groups { get; set; }
    }
}
