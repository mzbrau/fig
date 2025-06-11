using System;
using System.Collections.Generic;
using System.Linq;

namespace Fig.Contracts.ImportExport
{
    public class FigDataExportDataContract
    {
        public FigDataExportDataContract(
            DateTime exportedAt, 
            ImportType importType, 
            int version, 
            List<SettingClientExportDataContract> clients)
        {
            ExportedAt = exportedAt;
            ImportType = importType;
            Version = version;
            Clients = clients.OrderBy(a => a.Name).ToList();
        }

        public DateTime ExportedAt { get; set; }
        
        public ImportType ImportType { get; set; }
        
        public int Version { get; set; }

        public string? ExportingServer { get; set; }

        public string? Environment { get; set; }
        
        public List<SettingClientExportDataContract> Clients { get; set; }
    }
}