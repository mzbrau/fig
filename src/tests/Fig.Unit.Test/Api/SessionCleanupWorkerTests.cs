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
public class SessionCleanupWorkerTests
{
    [Test]
    public async Task ExecuteAsync_PerformsCleanupOnceOnStartup()
    {
        var cleanedUp = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var sessionCleanupService = new Mock<ISessionCleanupService>();
        sessionCleanupService
            .Setup(s => s.RemoveExpiredSessionsAsync())
            .Returns(Task.FromResult(0))
            .Callback(() => cleanedUp.TrySetResult());

        var services = new ServiceCollection();
        services.AddScoped(_ => sessionCleanupService.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var timerFactory = new Mock<ITimerFactory>();
        timerFactory.Setup(t => t.Create(It.IsAny<TimeSpan>())).Returns(new StubPeriodicTimer());

        using var worker = new SessionCleanupWorker(
            NullLogger<SessionCleanupWorker>.Instance,
            timerFactory.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            startupDelay: TimeSpan.Zero,
            cleanupInterval: TimeSpan.FromMinutes(1));

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await cleanedUp.Task.WaitAsync(TimeSpan.FromSeconds(2));
            sessionCleanupService.Verify(s => s.RemoveExpiredSessionsAsync(), Times.Once);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public async Task PerformCleanup_InvokesSessionCleanupService()
    {
        var sessionCleanupService = new Mock<ISessionCleanupService>();
        sessionCleanupService.Setup(s => s.RemoveExpiredSessionsAsync()).Returns(Task.FromResult(0));

        var services = new ServiceCollection();
        services.AddScoped(_ => sessionCleanupService.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var timerFactory = new Mock<ITimerFactory>();
        timerFactory.Setup(t => t.Create(It.IsAny<TimeSpan>())).Returns(new StubPeriodicTimer());

        var worker = new SessionCleanupWorker(
            NullLogger<SessionCleanupWorker>.Instance,
            timerFactory.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            startupDelay: TimeSpan.Zero,
            cleanupInterval: TimeSpan.FromMinutes(1));

        await worker.PerformCleanup();

        sessionCleanupService.Verify(s => s.RemoveExpiredSessionsAsync(), Times.Once);
    }

    private sealed class StubPeriodicTimer : IPeriodicTimer
    {
        public ValueTask<bool> WaitForNextTickAsync(CancellationToken token) => ValueTask.FromResult(false);

        public void Dispose()
        {
        }
    }
}
