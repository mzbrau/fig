using System;
using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class FigValueOnlyDataExportDataContract
    {
        public FigValueOnlyDataExportDataContract(
            DateTime exportedAt, 
            ImportType importType, 
            int version, 
            List<SettingClientValueExportDataContract> clients)
        {
            ExportedAt = exportedAt;
            ImportType = importType;
            Version = version;
            Clients = clients;
        }
        
        public DateTime ExportedAt { get; }
        
        public ImportType ImportType { get; }
        
        public int Version { get; }
        
        public List<SettingClientValueExportDataContract> Clients { get; }
    }
}