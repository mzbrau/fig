using Fig.Client.Health;
using Fig.Client.Workers;
using Fig.Contracts.Health;
using Fig.Unit.Test.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigHealthWorkerTests
{
    [SetUp]
    public void SetUp()
    {
        HealthCheckBridge.GetHealthReportAsync = null;
    }

    [TearDown]
    public void TearDown()
    {
        HealthCheckBridge.GetHealthReportAsync = null;
    }

    [Test]
    public async Task StartAsync_RegistersHealthReportProvider()
    {
        using var services = BuildServices();
        var worker = CreateWorker(services);

        await worker.StartAsync(CancellationToken.None);

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Not.Null);
    }

    [Test]
    public async Task StopAsync_ClearsOwnedHealthReportProvider()
    {
        using var services = BuildServices();
        var worker = CreateWorker(services);

        await worker.StartAsync(CancellationToken.None);
        await worker.StopAsync(CancellationToken.None);

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Null);
    }

    [Test]
    public async Task Dispose_ClearsOwnedHealthReportProvider()
    {
        using var services = BuildServices();
        var worker = CreateWorker(services);

        await worker.StartAsync(CancellationToken.None);
        worker.Dispose();

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Null);
    }

    [Test]
    public async Task StopAsync_WhenBridgeWasReplaced_DoesNotClearReplacementProvider()
    {
        using var services = BuildServices();
        var worker = CreateWorker(services);
        Func<Task<HealthDataContract>> replacementProvider = CreateHealthReport;

        await worker.StartAsync(CancellationToken.None);
        HealthCheckBridge.Register(replacementProvider);
        await worker.StopAsync(CancellationToken.None);

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.SameAs(replacementProvider));
    }

    [Test]
    public async Task StartAsync_WhenCalledTwice_ReplacesPreviousProvider()
    {
        using var services = BuildServices();
        var worker = CreateWorker(services);

        await worker.StartAsync(CancellationToken.None);
        var firstProvider = HealthCheckBridge.GetHealthReportAsync;
        await worker.StartAsync(CancellationToken.None);

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Not.Null);
        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Not.SameAs(firstProvider));
    }

    private static ServiceProvider BuildServices()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddHealthChecks().AddCheck("ready", () => HealthCheckResult.Healthy("ok"));
        return services.BuildServiceProvider();
    }

    private static FigHealthWorker<SimpleSettings> CreateWorker(IServiceProvider services)
    {
        var lifetime = new Mock<IHostApplicationLifetime>();
        lifetime.SetupGet(a => a.ApplicationStopping).Returns(CancellationToken.None);

        return new FigHealthWorker<SimpleSettings>(
            services.GetRequiredService<HealthCheckService>(),
            NullLogger<FigHealthWorker<SimpleSettings>>.Instance,
            lifetime.Object);
    }

    private static Task<HealthDataContract> CreateHealthReport()
    {
        return Task.FromResult(new HealthDataContract
        {
            Status = FigHealthStatus.Healthy,
            Components = []
        });
    }
}
