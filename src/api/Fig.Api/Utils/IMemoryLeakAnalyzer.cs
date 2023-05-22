using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public interface IMemoryLeakAnalyzer
{
    MemoryUsageAnalysisBusinessEntity? AnalyzeMemoryUsage(ClientRunSessionBusinessEntity clientRunSessionBusinessEntity);
}