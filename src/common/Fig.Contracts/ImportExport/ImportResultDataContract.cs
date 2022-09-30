using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class ImportResultDataContract
    {
        public ImportType ImportType { get; set; }

        public int ImportedClientCount { get; set; }

        public int DeletedClientCount { get; set; }

        public int DeferredImportClientCount { get; set; } = 0;

        public List<string> ImportedClients { get; set; } = new List<string>();

        public List<string> DeferredImportClients { get; set; } = new List<string>();

        public override string ToString()
        {
            return $"ImportType:{ImportType}, " +
                   $"ImportedClientCount:{ImportedClientCount}, " +
                   $"DeletedClientCount:{DeletedClientCount}, " +
                   $"DeferredClientCount:{DeferredImportClientCount}, " +
                   $"ImportedClients:[{string.Join(", ", ImportedClients)}]" +
                   $"DeferredImportClients:[{string.Join(", ", DeferredImportClients)}]";
        }
    }
}