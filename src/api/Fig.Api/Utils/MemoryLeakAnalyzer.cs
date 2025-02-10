using Fig.Api.Datalayer.Repositories;
using Fig.Datalayer.BusinessEntities;

namespace Fig.Api.Utils;

public class MemoryLeakAnalyzer : IMemoryLeakAnalyzer
{
    private readonly IConfigurationRepository _configurationRepository;

    public MemoryLeakAnalyzer(IConfigurationRepository configurationRepository)
    {
        _configurationRepository = configurationRepository;
    }
    
    public async Task<MemoryUsageAnalysisBusinessEntity?> AnalyzeMemoryUsage(ClientRunSessionBusinessEntity runSession)
    {
        var configuration = await _configurationRepository.GetConfiguration();
        
        if (!IsEligibleForMemoryLeakCheck(runSession, configuration))
            return null;
        
        // We skip the first X records to avoid any volatility during start up.
        var recordsToSkip = (int)(configuration.DelayBeforeMemoryLeakMeasurementsMs / runSession.PollIntervalMs);
        var allValidRecords = runSession.HistoricalMemoryUsage.OrderBy(a => a.ClientRunTimeSeconds)
            .Skip(recordsToSkip)
            .ToList();

        var (average, stdDev, startingAvg, endingAvg) = AnalyzeData(allValidRecords.Select(a => a.MemoryUsageBytes).ToList());

        var validRange = GetValidRange(average, stdDev);
        var recordsToCheck = RemoveOutliers(allValidRecords, validRange).ToList();

        return new MemoryUsageAnalysisBusinessEntity
        {
            TimeOfAnalysisUtc = DateTime.UtcNow,
            TrendLineSlope = GetTrendLine(recordsToCheck),
            Average = average,
            StandardDeviation = stdDev,
            StartingBytesAverage = startingAvg,
            EndingBytesAverage = endingAvg,
            SecondsAnalyzed = Convert.ToInt32(recordsToCheck.Last().ClientRunTimeSeconds - recordsToCheck.First().ClientRunTimeSeconds),
            DataPointsAnalyzed = recordsToCheck.Count
        };
    }

    private IEnumerable<MemoryUsageBusinessEntity> RemoveOutliers(IEnumerable<MemoryUsageBusinessEntity> records,
        (double Min, double Max) validRange)
    {
        return records.Where(record =>
            record.MemoryUsageBytes <= validRange.Max && 
            record.MemoryUsageBytes >= validRange.Min);
    }

    private bool IsEligibleForMemoryLeakCheck(ClientRunSessionBusinessEntity runSession,
        FigConfigurationBusinessEntity configuration)
    {
        if (runSession.MemoryAnalysis?.PossibleMemoryLeakDetected == true)
            return false;

        if (IsWithinInitialPeriodOfRuntime())
            return false; // We wait some time before our first check.

        if (IsLessThanConfiguredIntervalSinceLastCheck())
            return false; // Subsequent tests every configured interval

        if (!HasSufficientDataPoints())
            return false;
        
        return true;

        bool IsWithinInitialPeriodOfRuntime() =>
            runSession.MemoryAnalysis is null &&
            (DateTime.UtcNow - runSession.StartTimeUtc).TotalSeconds < TimeSpan.FromMilliseconds(configuration.DelayBeforeMemoryLeakMeasurementsMs + configuration.IntervalBetweenMemoryLeakChecksMs).TotalSeconds;

        bool IsLessThanConfiguredIntervalSinceLastCheck() =>
            runSession.MemoryAnalysis is not null &&
            DateTime.UtcNow - runSession.MemoryAnalysis.TimeOfAnalysisUtc < TimeSpan.FromMilliseconds(configuration.IntervalBetweenMemoryLeakChecksMs);

        bool HasSufficientDataPoints() => runSession.HistoricalMemoryUsage.Count > configuration.MinimumDataPointsForMemoryLeakCheck;
    }

    private static (double average, double stdDev, double startingAvg, double endingAvg) AnalyzeData(List<long> values)
    {
        var avg = values.Average();
        var stdDev = Math.Sqrt(values.Average(v=>Math.Pow(v-avg,2)));

        var startingAvg = values.Take(10).Average();
        var endingAvg = values.TakeLast(10).Average();
        
        return (avg, stdDev, startingAvg, endingAvg);
    }
    
    private (double min, double max) GetValidRange(double average, double stdDev)
    {
        var limit = stdDev * 2;
        return (average - limit, average + limit);
    }

    private double GetTrendLine(List<MemoryUsageBusinessEntity> records)
    {
        var numberOfRecords = records.Count;
        var sumX = records.Sum(x => x.ClientRunTimeSeconds);
        var sumX2 = records.Sum(x => x.ClientRunTimeSeconds * x.ClientRunTimeSeconds);
        var sumY = records.Sum(x => x.MemoryUsageBytes);
        var sumXy = records.Sum(x => x.ClientRunTimeSeconds * x.MemoryUsageBytes);
        
        var slope = (sumXy - ((sumX * sumY) / numberOfRecords )) / (sumX2 - (sumX * sumX / numberOfRecords));

        return slope;
    }
}