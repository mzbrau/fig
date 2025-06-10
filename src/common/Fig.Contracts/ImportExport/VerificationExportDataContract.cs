using System;
using System.Collections.Generic;

namespace Fig.Contracts.ImportExport;

[Obsolete("Removed in Fig 2.0")]
public class VerificationExportDataContract
{
    public VerificationExportDataContract(string name, IList<string>? propertyArguments)
    {
        Name = name;
        PropertyArguments = propertyArguments;
    }

    public string Name { get; set; }

    public IList<string>? PropertyArguments { get; set; }
}