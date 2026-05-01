using Fig.Client.Health;
using Fig.Contracts.Health;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class HealthCheckBridgeTests
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
    public void Register_SetsHealthReportProvider()
    {
        Func<Task<HealthDataContract>> provider = CreateHealthReport;

        HealthCheckBridge.Register(provider);

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.SameAs(provider));
    }

    [Test]
    public void ClearIfRegistered_WhenProviderMatches_ClearsHealthReportProvider()
    {
        Func<Task<HealthDataContract>> provider = CreateHealthReport;
        HealthCheckBridge.Register(provider);

        HealthCheckBridge.ClearIfRegistered(provider);

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Null);
    }

    [Test]
    public void ClearIfRegistered_WhenProviderWasReplaced_DoesNotClearNewProvider()
    {
        Func<Task<HealthDataContract>> originalProvider = () => CreateHealthReport();
        Func<Task<HealthDataContract>> replacementProvider = () => CreateHealthReport();
        HealthCheckBridge.Register(originalProvider);
        HealthCheckBridge.Register(replacementProvider);

        HealthCheckBridge.ClearIfRegistered(originalProvider);

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.SameAs(replacementProvider));
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
