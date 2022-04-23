using System;
using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class FigDataExportDataContract
    {
        public DateTime ExportedAt { get; set; }
        
        public ImportType ImportType { get; set; }
        
        public int Version { get; set; }
        
        public List<SettingClientExportDataContract> Clients { get; set; }
    }
}