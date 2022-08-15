namespace Fig.Common.NetStandard.Diag;

public interface IDiagnostics
{
    long GetMemoryUsageBytes();

    string GetRunningUser();
}