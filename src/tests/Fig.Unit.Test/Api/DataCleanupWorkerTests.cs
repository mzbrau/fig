using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Api.Services;
using Fig.Api.Workers;
using Fig.Common.Timer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class DataCleanupWorkerTests
{
    [Test]
    public async Task ExecuteAsync_PerformsCleanupOnceOnStartup()
    {
        var cleanedUp = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var cleanupService = new Mock<IDataCleanupService>();
        cleanupService
            .Setup(s => s.PerformCleanupAsync())
            .Returns(Task.FromResult(0))
            .Callback(() => cleanedUp.TrySetResult());

        var services = new ServiceCollection();
        services.AddScoped(_ => cleanupService.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var timerFactory = new Mock<ITimerFactory>();
        timerFactory.Setup(t => t.Create(It.IsAny<TimeSpan>())).Returns(new StubPeriodicTimer());

        using var worker = new DataCleanupWorker(
            NullLogger<DataCleanupWorker>.Instance,
            timerFactory.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            startupDelay: TimeSpan.Zero,
            cleanupInterval: TimeSpan.FromHours(1));

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await cleanedUp.Task.WaitAsync(TimeSpan.FromSeconds(2));
            cleanupService.Verify(s => s.PerformCleanupAsync(), Times.Once);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task PerformCleanup_InvokesDataCleanupService()
    {
        var cleanupService = new Mock<IDataCleanupService>();
        cleanupService.Setup(s => s.PerformCleanupAsync()).Returns(Task.FromResult(0));

        var services = new ServiceCollection();
        services.AddScoped(_ => cleanupService.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var timerFactory = new Mock<ITimerFactory>();
        timerFactory.Setup(t => t.Create(It.IsAny<TimeSpan>())).Returns(new StubPeriodicTimer());

        var worker = new DataCleanupWorker(
            NullLogger<DataCleanupWorker>.Instance,
            timerFactory.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            startupDelay: TimeSpan.Zero,
            cleanupInterval: TimeSpan.FromHours(1));

        await worker.PerformCleanup();

        cleanupService.Verify(s => s.PerformCleanupAsync(), Times.Once);
    }

    [Test]
    public async Task PerformCleanup_SwallowsServiceExceptions()
    {
        var cleanupService = new Mock<IDataCleanupService>();
        cleanupService
            .Setup(s => s.PerformCleanupAsync())
            .ThrowsAsync(new InvalidOperationException("cleanup failed"));

        var services = new ServiceCollection();
        services.AddScoped(_ => cleanupService.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var timerFactory = new Mock<ITimerFactory>();
        timerFactory.Setup(t => t.Create(It.IsAny<TimeSpan>())).Returns(new StubPeriodicTimer());

        var worker = new DataCleanupWorker(
            NullLogger<DataCleanupWorker>.Instance,
            timerFactory.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            startupDelay: TimeSpan.Zero,
            cleanupInterval: TimeSpan.FromHours(1));

        Assert.DoesNotThrowAsync(async () => await worker.PerformCleanup());
    }

    private sealed class StubPeriodicTimer : IPeriodicTimer
    {
        public ValueTask<bool> WaitForNextTickAsync(CancellationToken token) => ValueTask.FromResult(false);

        public void Dispose()
        {
        }
    }
}
