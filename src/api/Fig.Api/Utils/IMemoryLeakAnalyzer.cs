using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public interface IMemoryLeakAnalyzer
{
    Task<MemoryUsageAnalysisBusinessEntity?> AnalyzeMemoryUsage(ClientRunSessionBusinessEntity clientRunSessionBusinessEntity);
}