using System;
using System.Threading;
using System.Threading.Tasks;
using Fig.Api;
using Fig.Api.Constants;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.Utils;
using Fig.Api.Workers;
using Fig.Common.Events;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class SettingsReadSideEffectWorkerTests
{
    [Test]
    public async Task PublishAsync_WhenSideEffectQueued_TouchesSessionAndWritesEvent()
    {
        var processed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientId = Guid.NewGuid();
        var runSessionId = Guid.NewGuid();
        var loadedUtc = DateTime.UtcNow;

        var runSessionRepository = new Mock<IClientRunSessionRepository>();
        runSessionRepository.Setup(a => a.TouchLastSettingLoadUtc(runSessionId, loadedUtc))
            .Returns(Task.CompletedTask);

        var eventLogRepository = new Mock<IEventLogRepository>();
        eventLogRepository.Setup(a => a.Add(It.IsAny<EventLogBusinessEntity>()))
            .Returns(Task.CompletedTask)
            .Callback(() => processed.TrySetResult());

        var eventLogFactory = new Mock<IEventLogFactory>();
        eventLogFactory.Setup(a => a.SettingsRead(clientId, "AspNetApi", null))
            .Returns(new EventLogBusinessEntity());

        var services = new ServiceCollection();
        services.AddScoped<IClientRunSessionRepository>(_ => runSessionRepository.Object);
        services.AddScoped<IEventLogRepository>(_ => eventLogRepository.Object);
        services.AddScoped<IEventLogFactory>(_ => eventLogFactory.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var eventDistributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        var worker = new SettingsReadSideEffectWorker(
            eventDistributor,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SettingsReadSideEffectWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await eventDistributor.PublishAsync(
                EventConstants.SettingsReadSideEffect,
                new SettingsReadSideEffect(clientId, "AspNetApi", null, runSessionId, loadedUtc));

            await processed.Task.WaitAsync(TimeSpan.FromSeconds(1));

            runSessionRepository.Verify(a => a.TouchLastSettingLoadUtc(runSessionId, loadedUtc), Times.Once);
            eventLogFactory.Verify(a => a.SettingsRead(clientId, "AspNetApi", null), Times.Once);
            eventLogRepository.Verify(a => a.Add(It.IsAny<EventLogBusinessEntity>()), Times.Once);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
    }

    [Test]
    public async Task PublishAsync_WhenRunSessionIdEmpty_SkipsSessionTouch()
    {
        var processed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var clientId = Guid.NewGuid();

        var runSessionRepository = new Mock<IClientRunSessionRepository>(MockBehavior.Strict);

        var eventLogRepository = new Mock<IEventLogRepository>();
        eventLogRepository.Setup(a => a.Add(It.IsAny<EventLogBusinessEntity>()))
            .Returns(Task.CompletedTask)
            .Callback(() => processed.TrySetResult());

        var eventLogFactory = new Mock<IEventLogFactory>();
        eventLogFactory.Setup(a => a.SettingsRead(clientId, "AspNetApi", "one"))
            .Returns(new EventLogBusinessEntity());

        var services = new ServiceCollection();
        services.AddScoped<IClientRunSessionRepository>(_ => runSessionRepository.Object);
        services.AddScoped<IEventLogRepository>(_ => eventLogRepository.Object);
        services.AddScoped<IEventLogFactory>(_ => eventLogFactory.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var eventDistributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        var worker = new SettingsReadSideEffectWorker(
            eventDistributor,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<SettingsReadSideEffectWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await eventDistributor.PublishAsync(
                EventConstants.SettingsReadSideEffect,
                new SettingsReadSideEffect(clientId, "AspNetApi", "one", Guid.Empty, DateTime.UtcNow));

            await processed.Task.WaitAsync(TimeSpan.FromSeconds(1));

            eventLogRepository.Verify(a => a.Add(It.IsAny<EventLogBusinessEntity>()), Times.Once);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
    }

    [Test]
    public async Task PublishAsync_AfterStop_LogsDroppedSideEffectAndDoesNotCreateScope()
    {
        var eventDistributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        var serviceScopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        var logger = new Mock<ILogger<SettingsReadSideEffectWorker>>();
        var worker = new SettingsReadSideEffectWorker(
            eventDistributor,
            serviceScopeFactory.Object,
            logger.Object);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        try
        {
            await eventDistributor.PublishAsync(
                EventConstants.SettingsReadSideEffect,
                new SettingsReadSideEffect(Guid.NewGuid(), "AspNetApi", null, Guid.NewGuid(), DateTime.UtcNow));

            serviceScopeFactory.Verify(a => a.CreateScope(), Times.Never);
            logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Dropped settings-read side effects")),
                    It.IsAny<Exception?>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            worker.Dispose();
        }
    }
}
