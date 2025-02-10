using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Datalayer.BusinessEntities;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Utils;

[TestFixture]
public class MemoryLeakAnalyzerTests
{
    private readonly Random _random = new();
    private IMemoryLeakAnalyzer _memoryLeakAnalyzer = null!;
    private int _runtimeSeconds = 1;
    private FigConfigurationBusinessEntity _configuration = null!;
    

    [SetUp]
    public void Setup()
    {
        Mock<IConfigurationRepository> configurationRepositoryMock = new Mock<IConfigurationRepository>();
        _configuration = new FigConfigurationBusinessEntity
        {
            DelayBeforeMemoryLeakMeasurementsMs = 1000,
            IntervalBetweenMemoryLeakChecksMs = 1000
        };
        configurationRepositoryMock.Setup(a => a.GetConfiguration(false)).ReturnsAsync(_configuration);
        _memoryLeakAnalyzer = new MemoryLeakAnalyzer(configurationRepositoryMock.Object);
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task ShallDetectMemoryLeakWhenMemoryIncreases(bool variationInData)
    {
        var runSession = new ClientRunSessionBusinessEntity
        {
            StartTimeUtc = DateTime.UtcNow - TimeSpan.FromSeconds(10),
            PollIntervalMs = 5000
        };
        foreach (var record in Get100ResultsWithIncreasingMemory(variationInData))
            runSession.HistoricalMemoryUsage.Add(record);

        var result = await _memoryLeakAnalyzer.AnalyzeMemoryUsage(runSession);
        
        Assert.That(result?.PossibleMemoryLeakDetected, Is.True);
    }

    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task ShallNotDetectLeakWhenMemoryDecreases(bool variationInData)
    {
        var runSession = new ClientRunSessionBusinessEntity
        {
            StartTimeUtc = DateTime.UtcNow - TimeSpan.FromSeconds(10),
            PollIntervalMs = 5000
        };
        foreach (var record in Get100ResultsWithDecreasingMemory(variationInData))
            runSession.HistoricalMemoryUsage.Add(record);

        var result = await _memoryLeakAnalyzer.AnalyzeMemoryUsage(runSession);
        
        Assert.That(result?.PossibleMemoryLeakDetected, Is.False);
    }
    
    [Test]
    [TestCase(true)]
    [TestCase(false)]
    public async Task ShallNotDetectLeakWhenMemoryIsStable(bool variationInData)
    {
        var runSession = new ClientRunSessionBusinessEntity
        {
            StartTimeUtc = DateTime.UtcNow - TimeSpan.FromSeconds(10),
            PollIntervalMs = 5000
        };
        foreach (var record in Get100ResultsWithStableMemory(variationInData))
            runSession.HistoricalMemoryUsage.Add(record);

        var result = await _memoryLeakAnalyzer.AnalyzeMemoryUsage(runSession);
        
        Assert.That(result?.PossibleMemoryLeakDetected, Is.False);
    }

    private IEnumerable<MemoryUsageBusinessEntity> Get100ResultsWithStableMemory(bool addVariation)
    {
        for (var i = 0; i < 100; i++)
        {
            var variation = addVariation ? _random.Next(0, 20) : 0;
            yield return GetIncremented(50 + variation);
        }
    }
    
    private IEnumerable<MemoryUsageBusinessEntity> Get100ResultsWithIncreasingMemory(bool addVariation)
    {
        var memory = 10;
        for (var i = 0; i < 100; i++)
        {
            var variation = addVariation ? _random.Next(0, 10) : 0;
            yield return GetIncremented(memory++ + variation);
        }
    }
    
    private IEnumerable<MemoryUsageBusinessEntity> Get100ResultsWithDecreasingMemory(bool addVariation)
    {
        var memory = 1000;
        for (var i = 0; i < 100; i++)
        {
            var variation = addVariation ? _random.Next(0, 10) : 0;
            yield return GetIncremented(memory-- + variation);
        }
    }

    private MemoryUsageBusinessEntity GetIncremented(long memoryUsage)
    {
        return new MemoryUsageBusinessEntity()
        {
            ClientRunTimeSeconds = _runtimeSeconds++, 
            MemoryUsageBytes = memoryUsage
        };
    }
}