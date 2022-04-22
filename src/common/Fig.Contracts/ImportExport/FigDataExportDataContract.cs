using System;
using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class FigDataExportDataContract
    {
        private DateTime ExportedAt { get; set; }
        
        private ImportType ImportType { get; set; }
        
        public int Version { get; set; }
        
        public List<SettingClientExportDataContract> Clients { get; set; }
    }
}