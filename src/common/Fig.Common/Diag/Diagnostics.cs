using System;
using System.Diagnostics;

namespace Fig.Common.Diag;

public class Diagnostics : IDiagnostics
{
    public long GetMemoryUsageBytes()
    {
        return Environment.WorkingSet;
    }

    public string GetRunningUser()
    {
        return Environment.UserName;
    }
}