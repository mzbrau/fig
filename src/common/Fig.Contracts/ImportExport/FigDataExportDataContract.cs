using System;
using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class FigDataExportDataContract
    {
        public FigDataExportDataContract(DateTime exportedAt, ImportType importType, int version, List<SettingClientExportDataContract> clients)
        {
            ExportedAt = exportedAt;
            ImportType = importType;
            Version = version;
            Clients = clients;
        }

        public DateTime ExportedAt { get; set; }
        
        public ImportType ImportType { get; set; }
        
        public int Version { get; set; }
        
        public List<SettingClientExportDataContract> Clients { get; set; }
    }
}