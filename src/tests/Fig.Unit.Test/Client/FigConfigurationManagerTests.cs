using Fig.Client.Configuration;
using Fig.Client.Exceptions;
using Fig.Client.Health;
using Fig.Client.NetFramework;
using Fig.Contracts.Health;
using Fig.Unit.Test.TestInfrastructure;
using Microsoft.Extensions.Logging.Abstractions;
using NUnit.Framework;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class FigConfigurationManagerTests
{
    private string? _originalFigApiUri;

    [SetUp]
    public void SetUp()
    {
        _originalFigApiUri = Environment.GetEnvironmentVariable("FIG_API_URI");
        Environment.SetEnvironmentVariable("FIG_API_URI", null);
        FigConfigurationManager<SimpleSettings>.Reset();
        HealthCheckBridge.GetHealthReportAsync = null;
    }

    [TearDown]
    public void TearDown()
    {
        FigConfigurationManager<SimpleSettings>.Reset();
        HealthCheckBridge.GetHealthReportAsync = null;
        Environment.SetEnvironmentVariable("FIG_API_URI", _originalFigApiUri);
    }

    [Test]
    public void Settings_WhenNotInitialized_Throws()
    {
        Assert.Throws<NotInitializedException>(() =>
        {
            _ = FigConfigurationManager<SimpleSettings>.Settings;
        });
    }

    [Test]
    public void Reset_AfterInitialize_ClearsSettingsAndHealthBridge()
    {
        FigConfigurationManager<SimpleSettings>.Initialize(CreateOptions(), NullLogger.Instance);

        Assert.That(FigConfigurationManager<SimpleSettings>.Settings, Is.Not.Null);
        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Not.Null);

        FigConfigurationManager<SimpleSettings>.Reset();

        Assert.Throws<NotInitializedException>(() =>
        {
            _ = FigConfigurationManager<SimpleSettings>.Settings;
        });
        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Null);
    }

    [Test]
    public void Reset_WhenHealthBridgeWasReplaced_DoesNotClearReplacement()
    {
        FigConfigurationManager<SimpleSettings>.Initialize(CreateOptions(), NullLogger.Instance);
        Func<Task<HealthDataContract>> replacementProvider = () => Task.FromResult(new HealthDataContract
        {
            Status = FigHealthStatus.Healthy,
            Components = []
        });

        HealthCheckBridge.GetHealthReportAsync = replacementProvider;
        FigConfigurationManager<SimpleSettings>.Reset();

        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.SameAs(replacementProvider));
    }

    [Test]
    public void Initialize_WhenCalledTwice_ReplacesSettingsAndHealthBridge()
    {
        FigConfigurationManager<SimpleSettings>.Initialize(CreateOptions(), NullLogger.Instance);
        var firstSettings = FigConfigurationManager<SimpleSettings>.Settings;
        var firstHealthBridge = HealthCheckBridge.GetHealthReportAsync;

        FigConfigurationManager<SimpleSettings>.Initialize(CreateOptions(), NullLogger.Instance);

        Assert.That(FigConfigurationManager<SimpleSettings>.Settings, Is.Not.SameAs(firstSettings));
        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Not.Null);
        Assert.That(HealthCheckBridge.GetHealthReportAsync, Is.Not.SameAs(firstHealthBridge));
    }

    [Test]
    public void Reset_WhenCalledMultipleTimes_IsSafe()
    {
        FigConfigurationManager<SimpleSettings>.Initialize(CreateOptions(), NullLogger.Instance);

        Assert.DoesNotThrow(() =>
        {
            FigConfigurationManager<SimpleSettings>.Reset();
            FigConfigurationManager<SimpleSettings>.Reset();
        });
    }

    private static FigOptions CreateOptions()
    {
        return new FigOptions
        {
            ClientName = "TestClient",
            CommandLineArgs = []
        };
    }
}
