using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class ImportResultDataContract
    {
        public ImportType ImportType { get; set; }

        public List<string> ImportedClients { get; set; } = new();

        public List<string> DeferredImportClients { get; set; } = new();

        public List<string> DeletedClients { get; set; } = new();

        public string? ErrorMessage { get; set; }
    }
}