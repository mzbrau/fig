using System.Collections.Concurrent;
using BenchmarkDotNet.Attributes;

namespace Fig.Benchmarks.Test;

[MemoryDiagnoser]
public class RateMonitorBenchmarks
{
    private const int ItemCount = 100000;
    private const int ThresholdCount = 90000;
    
    [Benchmark]
    public void HashsetTest()
    {
        var hashset = new HashSet<long>();

        long limit = DateTime.UtcNow.Ticks;
        for (var i = 0; i < ItemCount; i++)
        {
            hashset.Add(DateTime.UtcNow.Ticks);
            if (i == ThresholdCount)
            {
                limit = DateTime.UtcNow.Ticks;
            }
        }

        var removedCount = hashset.RemoveWhere(a => a > limit);
    }
    
    [Benchmark]
    public void HashsetWithLocksTest()
    {
        var myLock = new object();
        var hashset = new HashSet<long>();

        long limit = DateTime.UtcNow.Ticks;
        for (var i = 0; i < ItemCount; i++)
        {
            lock (myLock)
            {
                hashset.Add(DateTime.UtcNow.Ticks);
            }
            
            if (i == ThresholdCount)
            {
                limit = DateTime.UtcNow.Ticks;
            }
        }

        lock (myLock)
        {
            var removedCount = hashset.RemoveWhere(a => a > limit);
        }
    }
    
    [Benchmark]
    public void ConcurrentDictionaryTest()
    {
        var dictionary = new ConcurrentDictionary<long, bool>();

        long limit = DateTime.UtcNow.Ticks;
        for (var i = 0; i < ItemCount; i++)
        {
            dictionary.TryAdd(DateTime.UtcNow.Ticks, false);
            //await Task.Delay(1);
            if (i == ThresholdCount)
            {
                limit = DateTime.UtcNow.Ticks;
            }
        }

        foreach (var item in dictionary.Keys.Where(a => a > limit))
            dictionary.TryRemove(item, out _);
    }
    
    [Benchmark]
    public void ConcurrentBagTest()
    {
        var bag = new ConcurrentBag<long>();

        long limit = DateTime.UtcNow.Ticks;
        for (var i = 0; i < ItemCount; i++)
        {
            bag.Add(DateTime.UtcNow.Ticks);
            //await Task.Delay(1);
            if (i == ThresholdCount)
            {
                limit = DateTime.UtcNow.Ticks;
            }
        }

        var result = bag.TakeWhile(a => a > limit);
    }
}