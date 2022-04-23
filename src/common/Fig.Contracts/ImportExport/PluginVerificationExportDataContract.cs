using System.Collections.Generic;

namespace Fig.Contracts.ImportExport
{
    public class PluginVerificationExportDataContract
    {
        public string Name { get; set; }

        public IList<string>? PropertyArguments { get; set; }
    }
}