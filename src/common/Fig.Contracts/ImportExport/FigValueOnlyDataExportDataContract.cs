using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Fig.Contracts.ImportExport
{
    public class FigValueOnlyDataExportDataContract
    {
        public FigValueOnlyDataExportDataContract(
            DateTime exportedAt, 
            ImportType importType, 
            int version, 
            bool? isExternallyManaged,
            List<SettingClientValueExportDataContract> clients)
        {
            ExportedAt = exportedAt;
            ImportType = importType;
            Version = version;
            Clients = clients;
            IsExternallyManaged = isExternallyManaged;
        }
        
        public DateTime ExportedAt { get; }
        
        public ImportType ImportType { get; internal set; }
        
        public int Version { get; }
        
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool? IsExternallyManaged { get; internal set; }
        
        public List<SettingClientValueExportDataContract> Clients { get; }
    }
}