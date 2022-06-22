using System.Diagnostics;

namespace Fig.Common.Diag;

public class Diagnostics : IDiagnostics
{
    public long GetMemoryUsageBytes()
    {
        using var proc = Process.GetCurrentProcess();
        return proc.PrivateMemorySize64;
    }
}