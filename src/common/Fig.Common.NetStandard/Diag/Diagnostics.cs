using System;

namespace Fig.Common.NetStandard.Diag;

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