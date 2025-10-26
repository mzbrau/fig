using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Fig.Api.Services;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class ClientRegistrationLockServiceTests
{
    private IClientRegistrationLockService _lockService = null!;
    
    // Volatile fields to ensure proper cross-thread visibility in concurrent tests
    private volatile bool _lock1Acquired;
    private volatile bool _lock2Acquired;
    private volatile bool _bothLocksHeldSimultaneously;

    [SetUp]
    public void SetUp()
    {
        _lockService = new ClientRegistrationLockService();
        
        // Reset volatile fields for each test
        _lock1Acquired = false;
        _lock2Acquired = false;
        _bothLocksHeldSimultaneously = false;
    }

    [Test]
    public async Task AcquireLockAsync_ShouldAllowDifferentClientsToAcquireLocksConcurrently()
    {
        // Arrange
        const string client1 = "client1";
        const string client2 = "client2";

        // Act
        var task1 = Task.Run(async () =>
        {
            using var lockHandle = await _lockService.AcquireLockAsync(client1);
            _lock1Acquired = true;
            await Task.Delay(100); // Hold the lock for a bit
            if (_lock2Acquired)
                _bothLocksHeldSimultaneously = true;
        });

        var task2 = Task.Run(async () =>
        {
            using var lockHandle = await _lockService.AcquireLockAsync(client2);
            _lock2Acquired = true;
            await Task.Delay(100); // Hold the lock for a bit
            if (_lock1Acquired)
                _bothLocksHeldSimultaneously = true;
        });

        await Task.WhenAll(task1, task2);

        // Assert
        Assert.That(_lock1Acquired, Is.True);
        Assert.That(_lock2Acquired, Is.True);
        Assert.That(_bothLocksHeldSimultaneously, Is.True, "Different clients should be able to hold locks simultaneously");
    }

    [Test]
    public async Task AcquireLockAsync_ShouldSerializeSameClientLocks()
    {
        // Arrange
        const string clientName = "testClient";
        var executionOrder = new ConcurrentBag<int>();
        var simultaneousExecutions = 0;
        var currentExecutions = 0;

        // Act
        var tasks = Enumerable.Range(0, 5).Select(i => Task.Run(async () =>
        {
            using var lockHandle = await _lockService.AcquireLockAsync(clientName);
            
            var current = Interlocked.Increment(ref currentExecutions);
            if (current > 1)
                Interlocked.Increment(ref simultaneousExecutions);
            
            executionOrder.Add(i);
            await Task.Delay(50); // Simulate work
            
            Interlocked.Decrement(ref currentExecutions);
        }));

        await Task.WhenAll(tasks);

        // Assert
        Assert.That(executionOrder.Count, Is.EqualTo(5), "All tasks should complete");
        Assert.That(simultaneousExecutions, Is.EqualTo(0), "No simultaneous executions should occur for the same client");
    }

    [Test]
    public async Task AcquireLockAsync_ShouldReleaseLockOnDispose()
    {
        // Arrange
        const string clientName = "testClient";
        var firstTaskCompleted = false;
        var secondTaskStarted = false;

        // Act
        var firstTask = Task.Run(async () =>
        {
            using (var lockHandle = await _lockService.AcquireLockAsync(clientName))
            {
                await Task.Delay(100);
            }
            firstTaskCompleted = true;
        });

        // Give first task time to acquire the lock
        await Task.Delay(50);

        var secondTask = Task.Run(async () =>
        {
            secondTaskStarted = true;
            using var lockHandle = await _lockService.AcquireLockAsync(clientName);
            Assert.That(firstTaskCompleted, Is.True, "First task should have completed and released lock");
        });

        await Task.WhenAll(firstTask, secondTask);

        // Assert
        Assert.That(secondTaskStarted, Is.True);
        Assert.That(firstTaskCompleted, Is.True);
    }

    [Test]
    public async Task AcquireLockAsync_ShouldHandleMultipleDisposeCalls()
    {
        // Arrange
        const string clientName = "testClient";

        // Act & Assert - should not throw
        var lockHandle = await _lockService.AcquireLockAsync(clientName);
        lockHandle.Dispose();
        lockHandle.Dispose(); // Second dispose should be safe
        
        // Verify lock was released by acquiring it again
        using var secondLock = await _lockService.AcquireLockAsync(clientName);
        Assert.That(secondLock, Is.Not.Null);
    }

    [Test]
    public async Task AcquireLockAsync_ShouldRespectCancellationToken()
    {
        // Arrange
        const string clientName = "testClient";
        using var cts = new CancellationTokenSource();
        
        // Acquire the lock first
        var firstLock = await _lockService.AcquireLockAsync(clientName);

        // Act & Assert
        cts.CancelAfter(100);
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
        {
            await _lockService.AcquireLockAsync(clientName, cts.Token);
        });
        
        firstLock.Dispose();
    }

    [Test]
    public async Task AcquireLockAsync_ShouldMaintainSeparateLocksPerClient()
    {
        // Arrange
        const string client1 = "client1";
        const string client2 = "client2";
        const string client3 = "client3";
        
        var executionTimes = new ConcurrentDictionary<string, (DateTime Start, DateTime End)>();

        // Act
        var tasks = new[]
        {
            Task.Run(async () => await ExecuteWithLock(client1)),
            Task.Run(async () => await ExecuteWithLock(client1)),
            Task.Run(async () => await ExecuteWithLock(client2)),
            Task.Run(async () => await ExecuteWithLock(client2)),
            Task.Run(async () => await ExecuteWithLock(client3))
        };

        await Task.WhenAll(tasks);

        // Assert
        Assert.That(executionTimes.Count, Is.EqualTo(5));
        
        // Verify that operations for the same client don't overlap
        var client1Times = executionTimes.Where(x => x.Key.StartsWith("client1")).Select(x => x.Value).ToList();
        Assert.That(client1Times.Count, Is.EqualTo(2));
        Assert.That(client1Times[0].End <= client1Times[1].Start || client1Times[1].End <= client1Times[0].Start,
            "Same client operations should not overlap");

        async Task ExecuteWithLock(string clientName)
        {
            var taskId = $"{clientName}_{Guid.NewGuid()}";
            using var lockHandle = await _lockService.AcquireLockAsync(clientName);
            
            var start = DateTime.UtcNow;
            await Task.Delay(50);
            var end = DateTime.UtcNow;
            
            executionTimes[taskId] = (start, end);
        }
    }

    [Test]
    public async Task AcquireLockAsync_ShouldHandleHighConcurrency()
    {
        // Arrange
        const int numberOfClients = 10;
        const int operationsPerClient = 10;
        var clientCounters = new ConcurrentDictionary<string, int>();
        var errors = new ConcurrentBag<Exception>();

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < numberOfClients; i++)
        {
            var clientName = $"client{i}";
            clientCounters[clientName] = 0;
            
            for (int j = 0; j < operationsPerClient; j++)
            {
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        using var lockHandle = await _lockService.AcquireLockAsync(clientName);
                        clientCounters[clientName]++;
                        await Task.Delay(10);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }));
            }
        }

        await Task.WhenAll(tasks);

        // Assert
        Assert.That(errors, Is.Empty, "No errors should occur during high concurrency");
        Assert.That(clientCounters.Count, Is.EqualTo(numberOfClients));
        foreach (var counter in clientCounters)
        {
            Assert.That(counter.Value, Is.EqualTo(operationsPerClient), 
                $"All operations for {counter.Key} should complete");
        }
    }

    [Test]
    public void AcquireLockAsync_ShouldThrowOnEmptyClientName()
    {
        Assert.That(async () => await _lockService.AcquireLockAsync(""), Throws.ArgumentException);
    }

    [Test]
    public async Task AcquireLockAsync_ShouldMeasureWaitTime()
    {
        // Arrange
        const string clientName = "testClient";

        // Acquire first lock
        var firstLock = await _lockService.AcquireLockAsync(clientName);

        // Act - try to acquire second lock (should wait)
        var waitTask = Task.Run(async () =>
        {
            var waitStopwatch = Stopwatch.StartNew();
            using var lockHandle = await _lockService.AcquireLockAsync(clientName);
            waitStopwatch.Stop();
            return waitStopwatch.ElapsedMilliseconds;
        });

        // Hold the first lock for 200ms
        await Task.Delay(200);
        firstLock.Dispose();

        var waitTime = await waitTask;

        // Assert
        Assert.That(waitTime, Is.GreaterThanOrEqualTo(100), 
            "Second lock should have waited for first lock to be released");
    }

    [Test]
    public void CleanupUnusedLocks_ShouldNotRemoveLocksInUse()
    {
        // Arrange
        var lockService = new ClientRegistrationLockService();
        const string clientName = "testClient";
        
        // Acquire and hold a lock
        var lockHandle = lockService.AcquireLockAsync(clientName).Result;

        // Act
        lockService.CleanupUnusedLocks();

        // Assert - should still be able to acquire the same lock after releasing
        lockHandle.Dispose();
        var secondLock = lockService.AcquireLockAsync(clientName).Result;
        Assert.That(secondLock, Is.Not.Null);
        secondLock.Dispose();
    }

    [Test]
    public async Task CleanupUnusedLocks_ShouldRemoveOldUnusedLocks()
    {
        // Arrange
        var lockService = new ClientRegistrationLockService();
        const string clientName = "oldClient";
        
        // Acquire and immediately release a lock
        using (var lockHandle = await lockService.AcquireLockAsync(clientName))
        {
            // Lock acquired and will be released
        }

        // Use reflection to modify the LastAccessTime to simulate an old lock
        var lockEntry = GetLockEntry(lockService, clientName);
        Assert.That(lockEntry, Is.Not.Null, "Lock entry should exist");
        
        SetLastAccessTime(lockEntry!, DateTime.UtcNow - TimeSpan.FromHours(1));

        // Act
        lockService.CleanupUnusedLocks();

        // Assert - lock should be removed, verify by checking internal state
        var lockEntryAfterCleanup = GetLockEntry(lockService, clientName);
        Assert.That(lockEntryAfterCleanup, Is.Null, "Old unused lock should be removed");
    }

    [Test]
    public async Task CleanupUnusedLocks_ShouldNotRemoveRecentlyUsedLocks()
    {
        // Arrange
        var lockService = new ClientRegistrationLockService();
        const string clientName = "recentClient";
        
        // Acquire and release a lock
        using (var lockHandle = await lockService.AcquireLockAsync(clientName))
        {
            // Lock acquired and will be released
        }

        // Act - cleanup immediately
        lockService.CleanupUnusedLocks();

        // Assert - lock should still exist because it was recently used
        var lockEntry = GetLockEntry(lockService, clientName);
        Assert.That(lockEntry, Is.Not.Null, "Recently used lock should not be removed");
    }

    [Test]
    public async Task CleanupUnusedLocks_ShouldHandleRaceCondition()
    {
        // Arrange
        var lockService = new ClientRegistrationLockService();
        const string clientName = "raceClient";
        
        // Acquire and immediately release a lock
        using (var lockHandle = await lockService.AcquireLockAsync(clientName))
        {
            // Lock acquired and will be released
        }

        // Make the lock old
        var lockEntry = GetLockEntry(lockService, clientName);
        SetLastAccessTime(lockEntry!, DateTime.UtcNow - TimeSpan.FromHours(1));

        // Act - start cleanup and simultaneously acquire the lock
        var cleanupTask = Task.Run(() => lockService.CleanupUnusedLocks());
        var acquireTask = Task.Run(async () => await lockService.AcquireLockAsync(clientName));

        await Task.WhenAll(cleanupTask, acquireTask);
        
        // Assert - should successfully acquire the lock regardless of cleanup
        var lockHandle2 = await acquireTask;
        Assert.That(lockHandle2, Is.Not.Null);
        lockHandle2.Dispose();
    }

    [Test]
    public async Task CleanupUnusedLocks_ShouldHandleMultipleClients()
    {
        // Arrange
        var lockService = new ClientRegistrationLockService();
        const string oldClient1 = "oldClient1";
        const string oldClient2 = "oldClient2";
        const string recentClient = "recentClient";
        const string activeClient = "activeClient";
        
        // Create old locks
        using (await lockService.AcquireLockAsync(oldClient1)) { }
        using (await lockService.AcquireLockAsync(oldClient2)) { }
        
        // Create recent lock
        using (await lockService.AcquireLockAsync(recentClient)) { }
        
        // Create and hold active lock
        var activeLock = await lockService.AcquireLockAsync(activeClient);
        
        // Make old locks actually old
        SetLastAccessTime(GetLockEntry(lockService, oldClient1)!, DateTime.UtcNow - TimeSpan.FromHours(1));
        SetLastAccessTime(GetLockEntry(lockService, oldClient2)!, DateTime.UtcNow - TimeSpan.FromHours(1));

        // Act
        lockService.CleanupUnusedLocks();

        // Assert
        Assert.That(GetLockEntry(lockService, oldClient1), Is.Null, "Old client 1 should be removed");
        Assert.That(GetLockEntry(lockService, oldClient2), Is.Null, "Old client 2 should be removed");
        Assert.That(GetLockEntry(lockService, recentClient), Is.Not.Null, "Recent client should not be removed");
        Assert.That(GetLockEntry(lockService, activeClient), Is.Not.Null, "Active client should not be removed");
        
        activeLock.Dispose();
    }

    private object? GetLockEntry(ClientRegistrationLockService lockService, string clientName)
    {
        var field = typeof(ClientRegistrationLockService).GetField("_clientLocks", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dict = field?.GetValue(lockService) as System.Collections.IDictionary;
        return dict?[clientName];
    }

    private void SetLastAccessTime(object lockEntry, DateTime time)
    {
        var property = lockEntry.GetType().GetProperty("LastAccessTime");
        var field = lockEntry.GetType().GetField("<LastAccessTime>k__BackingField",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(lockEntry, time);
    }
}
