using Fig.Client.Workers;
using Fig.Unit.Test.TestInfrastructure;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigRestartWorkerTests
{
    [Test]
    public async Task OnChange_WhenRestartRequested_StopsApplication()
    {
        var settings = new SimpleSettings { RestartRequested = true };
        var options = new TestOptionsMonitor<SimpleSettings>(settings);
        var lifetime = new Mock<IHostApplicationLifetime>();
        var worker = CreateWorker(options, lifetime);

        await worker.StartAsync(CancellationToken.None);
        options.TriggerChange();

        lifetime.Verify(a => a.StopApplication(), Times.Once);
    }

    [Test]
    public async Task StopAsync_DisposesChangeRegistration()
    {
        var settings = new SimpleSettings { RestartRequested = true };
        var options = new TestOptionsMonitor<SimpleSettings>(settings);
        var lifetime = new Mock<IHostApplicationLifetime>();
        var worker = CreateWorker(options, lifetime);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);
        options.TriggerChange();

        Assert.That(options.ListenerCount, Is.EqualTo(0));
        lifetime.Verify(a => a.StopApplication(), Times.Never);
    }

    [Test]
    public async Task Dispose_DisposesChangeRegistration()
    {
        var settings = new SimpleSettings { RestartRequested = true };
        var options = new TestOptionsMonitor<SimpleSettings>(settings);
        var lifetime = new Mock<IHostApplicationLifetime>();
        var worker = CreateWorker(options, lifetime);

        await worker.StartAsync(CancellationToken.None);
        worker.Dispose();
        options.TriggerChange();

        Assert.That(options.ListenerCount, Is.EqualTo(0));
        lifetime.Verify(a => a.StopApplication(), Times.Never);
    }

    [Test]
    public async Task StartAsync_WhenCalledTwice_ReplacesExistingRegistration()
    {
        var settings = new SimpleSettings { RestartRequested = true };
        var options = new TestOptionsMonitor<SimpleSettings>(settings);
        var lifetime = new Mock<IHostApplicationLifetime>();
        var worker = CreateWorker(options, lifetime);

        await worker.StartAsync(CancellationToken.None);
        await worker.StartAsync(CancellationToken.None);
        options.TriggerChange();

        Assert.That(options.ListenerCount, Is.EqualTo(1));
        lifetime.Verify(a => a.StopApplication(), Times.Once);
    }

    [Test]
    public async Task Dispose_WhenCalledMultipleTimes_IsSafe()
    {
        var options = new TestOptionsMonitor<SimpleSettings>(new SimpleSettings());
        var lifetime = new Mock<IHostApplicationLifetime>();
        var worker = CreateWorker(options, lifetime);

        await worker.StartAsync(CancellationToken.None);

        Assert.DoesNotThrow(() =>
        {
            worker.Dispose();
            worker.Dispose();
        });
        Assert.That(options.ListenerCount, Is.EqualTo(0));
    }

    private static FigRestartWorker<SimpleSettings> CreateWorker(
        TestOptionsMonitor<SimpleSettings> options,
        Mock<IHostApplicationLifetime> lifetime)
    {
        return new FigRestartWorker<SimpleSettings>(
            options,
            NullLogger<FigRestartWorker<SimpleSettings>>.Instance,
            lifetime.Object);
    }
}

