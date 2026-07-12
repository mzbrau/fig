using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Fig.Api;
using Fig.Api.Datalayer.Repositories;
using Fig.Api.ExtensionMethods;
using Fig.Api.Services;
using Fig.Api.Utils;
using Fig.Api.Workers;
using Fig.Contracts.Scheduling;
using Fig.Contracts.Settings;
using Fig.Datalayer.BusinessEntities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Api;

[TestFixture]
public class SchedulingWorkerTests
{
    [Test]
    public async Task ExecuteAsync_EvaluatesDeferredChangesImmediatelyOnStartup()
    {
        var evaluated = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var deferredChangeRepository = new Mock<IDeferredChangeRepository>();
        deferredChangeRepository
            .Setup(a => a.GetChangesToExecute(It.IsAny<DateTime>()))
            .ReturnsAsync([])
            .Callback(() => evaluated.TrySetResult());

        var services = new ServiceCollection();
        services.AddScoped<IDeferredChangeRepository>(_ => deferredChangeRepository.Object);
        services.AddScoped<ISettingsService>(_ => Mock.Of<ISettingsService>());
        await using var serviceProvider = services.BuildServiceProvider();

        var settingsMonitor = new Mock<IOptionsMonitor<ApiSettings>>();
        settingsMonitor.Setup(a => a.CurrentValue).Returns(new ApiSettings
        {
            DbConnectionString = "Data Source=:memory:",
            SchedulingCheckIntervalMs = 547
        });

        var worker = new SchedulingWorker(
            NullLogger<SchedulingWorker>.Instance,
            settingsMonitor.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await evaluated.Task.WaitAsync(TimeSpan.FromSeconds(1));
            deferredChangeRepository.Verify(a => a.GetChangesToExecute(It.IsAny<DateTime>()), Times.AtLeastOnce);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }

    [Test]
    public void GetIntervalMs_UsesConfiguredValueFromOptionsMonitor()
    {
        var settingsMonitor = new Mock<IOptionsMonitor<ApiSettings>>();
        settingsMonitor.Setup(a => a.CurrentValue).Returns(new ApiSettings
        {
            DbConnectionString = "Data Source=:memory:",
            SchedulingCheckIntervalMs = 547
        });

        var worker = new SchedulingWorker(
            NullLogger<SchedulingWorker>.Instance,
            settingsMonitor.Object,
            Mock.Of<IServiceScopeFactory>());

        var interval = worker.GetIntervalMsForTests();

        Assert.That(interval, Is.EqualTo(547));
    }

    [Test]
    public async Task ExecuteAsync_PersistsClearedApplyAtBeforeApplyingChange()
    {
        var applyAt = DateTime.UtcNow.AddMinutes(-1);
        var revertAt = applyAt.AddMinutes(5);
        var changeSet = new SettingValueUpdatesDataContract(
            [new SettingDataContract("AStringSetting", new StringSettingDataContract("temporary"))],
            "test",
            new ScheduleDataContract(applyAt, revertAt));
        var deferredChange = new DeferredChangeBusinessEntity
        {
            Id = Guid.NewGuid(),
            ClientName = "Client",
            ExecuteAtUtc = applyAt,
            ChangeSet = changeSet
        };

        DeferredChangeBusinessEntity? updatedEntity = null;
        var deferredChangeRepository = new Mock<IDeferredChangeRepository>();
        deferredChangeRepository
            .Setup(a => a.GetChangesToExecute(It.IsAny<DateTime>()))
            .ReturnsAsync([deferredChange]);
        deferredChangeRepository
            .Setup(a => a.UpdateDeferredChange(It.IsAny<DeferredChangeBusinessEntity>()))
            .Callback<DeferredChangeBusinessEntity>(entity => updatedEntity = entity)
            .Returns(Task.CompletedTask);
        deferredChangeRepository
            .Setup(a => a.Remove(It.IsAny<Guid>()))
            .Returns(Task.CompletedTask);

        var settingsService = new Mock<ISettingsService>();
        settingsService
            .Setup(a => a.UpdateSettingValues(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.IsAny<SettingValueUpdatesDataContract>()))
            .Returns(Task.CompletedTask);

        var services = new ServiceCollection();
        services.AddScoped<IDeferredChangeRepository>(_ => deferredChangeRepository.Object);
        services.AddScoped<ISettingsService>(_ => settingsService.Object);
        await using var serviceProvider = services.BuildServiceProvider();

        var settingsMonitor = new Mock<IOptionsMonitor<ApiSettings>>();
        settingsMonitor.Setup(a => a.CurrentValue).Returns(new ApiSettings
        {
            DbConnectionString = "Data Source=:memory:",
            SchedulingCheckIntervalMs = 547
        });

        var worker = new SchedulingWorker(
            NullLogger<SchedulingWorker>.Instance,
            settingsMonitor.Object,
            serviceProvider.GetRequiredService<IServiceScopeFactory>());

        await worker.StartAsync(CancellationToken.None);

        try
        {
            await Task.Delay(200);
            deferredChangeRepository.Verify(a => a.UpdateDeferredChange(It.IsAny<DeferredChangeBusinessEntity>()), Times.Once);
            Assert.That(updatedEntity, Is.Not.Null);
            Assert.That(updatedEntity!.ChangeSet?.Schedule?.ApplyAtUtc, Is.Null);
            Assert.That(updatedEntity.ChangeSet?.Schedule?.RevertAtUtc, Is.EqualTo(revertAt));
            settingsService.Verify(a => a.UpdateSettingValues(
                "Client",
                null,
                It.IsAny<SettingValueUpdatesDataContract>()), Times.Once);
            Assert.That(deferredChange.ChangeSet?.Schedule?.ApplyAtUtc, Is.Null);
        }
        finally
        {
            await worker.StopAsync(CancellationToken.None);
        }
    }
}
