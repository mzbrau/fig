using System;
using System.Threading;
using System.Threading.Tasks;
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
public class CheckpointTriggerWorkerTests
{
    [Test]
    public async Task PublishAsync_WhenTriggerQueued_PersistsTriggerThroughWorkerLoop()
    {
        var processed = new TaskCompletionSource<CheckPointTriggerBusinessEntity>(TaskCreationOptions.RunContinuationsAsynchronously);
        var repository = new Mock<ICheckPointTriggerRepository>();
        repository.Setup(a => a.AddTrigger(It.IsAny<CheckPointTriggerBusinessEntity>()))
            .Returns<CheckPointTriggerBusinessEntity>(entity =>
            {
                processed.TrySetResult(entity);
                return Task.CompletedTask;
            });

        var services = new ServiceCollection();
        services.AddScoped<ICheckPointTriggerRepository>(_ => repository.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var eventDistributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        var worker = new CheckpointTriggerWorker(
            eventDistributor,
            serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<CheckpointTriggerWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await eventDistributor.PublishAsync(EventConstants.CheckPointTrigger, new CheckPointTrigger("Settings updated", "alice"));

            var storedTrigger = await processed.Task.WaitAsync(TimeSpan.FromSeconds(1));

            Assert.That(storedTrigger.AfterEvent, Is.EqualTo("Settings updated"));
            Assert.That(storedTrigger.User, Is.EqualTo("alice"));
            Assert.That(storedTrigger.Timestamp, Is.Not.EqualTo(default(DateTime)));
            repository.Verify(a => a.AddTrigger(It.IsAny<CheckPointTriggerBusinessEntity>()), Times.Once);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
            worker.Dispose();
        }
    }

    [Test]
    public async Task StopAsync_WhenQueuedTriggerIsStillDelayed_DoesNotCreateScope()
    {
        var eventDistributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        var serviceScopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        var worker = new CheckpointTriggerWorker(
            eventDistributor,
            serviceScopeFactory.Object,
            NullLogger<CheckpointTriggerWorker>.Instance);

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await eventDistributor.PublishAsync(EventConstants.CheckPointTrigger, new CheckPointTrigger("Settings updated", "alice"));
            await worker.StopAsync(CancellationToken.None);
            await Task.Delay(100);

            serviceScopeFactory.Verify(a => a.CreateScope(), Times.Never);
        }
        finally
        {
            worker.Dispose();
        }
    }

    [Test]
    public async Task PublishAsync_AfterStop_LogsDroppedTriggerAndDoesNotCreateScope()
    {
        var eventDistributor = new EventDistributor(Mock.Of<ILogger<EventDistributor>>());
        var serviceScopeFactory = new Mock<IServiceScopeFactory>(MockBehavior.Strict);
        var logger = new Mock<ILogger<CheckpointTriggerWorker>>();
        var worker = new CheckpointTriggerWorker(
            eventDistributor,
            serviceScopeFactory.Object,
            logger.Object);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        try
        {
            await eventDistributor.PublishAsync(EventConstants.CheckPointTrigger, new CheckPointTrigger("Settings updated", "alice"));

            serviceScopeFactory.Verify(a => a.CreateScope(), Times.Never);
            logger.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Dropped checkpoint creation")),
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
