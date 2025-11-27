using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Contracts;
using Fig.Client.Health;
using Fig.Client.Status;
using Fig.Client.Versions;
using Fig.Common.NetStandard.Diag;
using Fig.Common.NetStandard.IpAddress;
using Fig.Contracts.Health;
using Fig.Contracts.Status;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Fig.Unit.Test.Client.Status;

[TestFixture]
public class SettingStatusMonitorTests
{
    private Mock<IIpAddressResolver> _ipAddressResolverMock = null!;
    private Mock<IVersionProvider> _versionProviderMock = null!;
    private Mock<IDiagnostics> _diagnosticsMock = null!;
    private Mock<IHttpClientFactory> _httpClientFactoryMock = null!;
    private Mock<IFigConfigurationSource> _configMock = null!;
    private Mock<IClientSecretProvider> _clientSecretProviderMock = null!;
    private Mock<ILogger<SettingStatusMonitor>> _loggerMock = null!;
    private Mock<HttpMessageHandler> _httpMessageHandlerMock = null!;
    private SettingStatusMonitor? _statusMonitor;
    private HttpClient _httpClient = null!;

    [SetUp]
    public void Setup()
    {
        _ipAddressResolverMock = new Mock<IIpAddressResolver>();
        _versionProviderMock = new Mock<IVersionProvider>();
        _diagnosticsMock = new Mock<IDiagnostics>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IFigConfigurationSource>();
        _clientSecretProviderMock = new Mock<IClientSecretProvider>();
        _loggerMock = new Mock<ILogger<SettingStatusMonitor>>();
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();

        _ipAddressResolverMock.Setup(x => x.Resolve()).Returns("127.0.0.1");
        _versionProviderMock.Setup(x => x.GetFigVersion()).Returns("1.0.0");
        _versionProviderMock.Setup(x => x.GetHostVersion()).Returns("1.0.0");
        _diagnosticsMock.Setup(x => x.GetRunningUser()).Returns("testuser");
        _diagnosticsMock.Setup(x => x.GetMemoryUsageBytes()).Returns(1024000);
        _configMock.Setup(x => x.ClientName).Returns("TestClient");
        _configMock.Setup(x => x.PollIntervalMs).Returns(5000);
        _configMock.Setup(x => x.AllowOfflineSettings).Returns(true);
        _configMock.Setup(x => x.Instance).Returns((string?)null);
        _clientSecretProviderMock.Setup(x => x.GetSecret("TestClient")).ReturnsAsync("test-secret");
        
        // Setup a mock HttpClient with proper mocking
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost/")
        };
        _httpClientFactoryMock.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(_httpClient);
    }

    [TearDown]
    public void TearDown()
    {
        _statusMonitor?.Dispose();
        _httpClient?.Dispose();
        HealthCheckBridge.GetHealthReportAsync = null;
        ExternallyManagedSettingsBridge.ExternallyManagedSettings = null;
    }

    [Test]
    public async Task SetFailedRegistration_ShouldInjectUnhealthyStatusIntoHealthReport()
    {
        // Arrange
        var originalHealthReport = new HealthDataContract
        {
            Status = FigHealthStatus.Healthy,
            Components =
            [
                new("Database", FigHealthStatus.Healthy, "Database is responsive"),
                new("ExternalService", FigHealthStatus.Healthy, "Service is available")
            ]
        };

        HealthCheckBridge.GetHealthReportAsync = () => Task.FromResult(originalHealthReport);

        // Mock HTTP response to prevent actual API calls
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(new StatusResponseDataContract
            {
                PollIntervalMs = 5000,
                SettingUpdateAvailable = false,
                AllowOfflineSettings = true,
                RestartRequested = false
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        _statusMonitor = new SettingStatusMonitor(
            _ipAddressResolverMock.Object,
            _versionProviderMock.Object,
            _diagnosticsMock.Object,
            _httpClientFactoryMock.Object,
            _configMock.Object,
            _clientSecretProviderMock.Object,
            _loggerMock.Object);

        var registrationFailureMessage = "Failed to register client: Internal server error";

        // Act
        _statusMonitor.SetFailedRegistration(registrationFailureMessage);
        await _statusMonitor.SyncStatus(); // This calls GetStatus() which applies the registration failure

        // Assert - Verify the HTTP request was made and check what was sent
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Put && req.RequestUri!.ToString().Contains("/statuses/")),
            ItExpr.IsAny<CancellationToken>());

        // Since we can't directly access the private health report processing,
        // we verify the behavior through the HTTP request that gets sent
        // The GetStatus method should have modified the health report before sending it
        var httpRequestMessage = GetCapturedHttpRequest();
        Assert.That(httpRequestMessage, Is.Not.Null, "Should have made HTTP request");
        
        var requestContent = await httpRequestMessage!.Content!.ReadAsStringAsync();
        var statusRequest = JsonConvert.DeserializeObject<StatusRequestDataContract>(requestContent);
        
        Assert.That(statusRequest, Is.Not.Null, "Should have valid status request");
        Assert.That(statusRequest!.Health, Is.Not.Null, "Should have health data in request");
        Assert.That(statusRequest.Health!.Status, Is.EqualTo(FigHealthStatus.Unhealthy), 
            "Health status should be Unhealthy due to registration failure");

        var registrationComponent = statusRequest.Health.Components.FirstOrDefault(c => c.Name == "Registration");
        Assert.That(registrationComponent, Is.Not.Null, 
            "Should have Registration component in health report");
        Assert.That(registrationComponent!.Status, Is.EqualTo(FigHealthStatus.Unhealthy), 
            "Registration component should be Unhealthy");
        Assert.That(registrationComponent.Message, Is.EqualTo(registrationFailureMessage), 
            "Registration component should contain the failure message");

        // Verify original components are preserved
        Assert.That(statusRequest.Health.Components.Count, Is.EqualTo(3), 
            "Should have original components plus registration component");
        Assert.That(statusRequest.Health.Components.Any(c => c.Name == "Database"), Is.True, 
            "Should preserve Database component");
        Assert.That(statusRequest.Health.Components.Any(c => c.Name == "ExternalService"), Is.True, 
            "Should preserve ExternalService component");
    }

    [Test]
    public async Task SyncStatus_WithoutRegistrationFailure_ShouldPreserveOriginalHealthStatus()
    {
        // Arrange
        var originalHealthReport = new HealthDataContract
        {
            Status = FigHealthStatus.Healthy,
            Components = [new("Database", FigHealthStatus.Healthy, "Database is responsive")]
        };

        HealthCheckBridge.GetHealthReportAsync = () => Task.FromResult(originalHealthReport);

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(new StatusResponseDataContract
            {
                PollIntervalMs = 5000,
                SettingUpdateAvailable = false,
                AllowOfflineSettings = true,
                RestartRequested = false
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        _statusMonitor = new SettingStatusMonitor(
            _ipAddressResolverMock.Object,
            _versionProviderMock.Object,
            _diagnosticsMock.Object,
            _httpClientFactoryMock.Object,
            _configMock.Object,
            _clientSecretProviderMock.Object,
            _loggerMock.Object);

        // Act - No registration failure set, just sync status
        await _statusMonitor.SyncStatus();

        // Assert
        var httpRequestMessage = GetCapturedHttpRequest();
        var requestContent = await httpRequestMessage!.Content!.ReadAsStringAsync();
        var statusRequest = JsonConvert.DeserializeObject<StatusRequestDataContract>(requestContent);
        
        Assert.That(statusRequest!.Health, Is.Not.Null, "Should have health data");
        Assert.That(statusRequest.Health!.Status, Is.EqualTo(FigHealthStatus.Healthy), 
            "Health status should remain Healthy when no registration failure");

        var registrationComponent = statusRequest.Health.Components.FirstOrDefault(c => c.Name == "Registration");
        Assert.That(registrationComponent, Is.Null, 
            "Should not have Registration component when no registration failure");

        Assert.That(statusRequest.Health.Components.Count, Is.EqualTo(1), 
            "Should only have original components");
    }

    [Test]
    public async Task SetFailedRegistration_WithNoHealthReport_ShouldNotThrow()
    {
        // Arrange
        HealthCheckBridge.GetHealthReportAsync = null; // No health report provider

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(new StatusResponseDataContract
            {
                PollIntervalMs = 5000
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        _statusMonitor = new SettingStatusMonitor(
            _ipAddressResolverMock.Object,
            _versionProviderMock.Object,
            _diagnosticsMock.Object,
            _httpClientFactoryMock.Object,
            _configMock.Object,
            _clientSecretProviderMock.Object,
            _loggerMock.Object);

        // Act & Assert - Should not throw when no health report is available
        Assert.DoesNotThrow(() => _statusMonitor.SetFailedRegistration("Registration failed"));
        Assert.DoesNotThrowAsync(async () => await _statusMonitor.SyncStatus());

        // Verify request was still made (health will be null in the status request)
        var httpRequestMessage = GetCapturedHttpRequest();
        var requestContent = await httpRequestMessage!.Content!.ReadAsStringAsync();
        var statusRequest = JsonConvert.DeserializeObject<StatusRequestDataContract>(requestContent);
        
        Assert.That(statusRequest!.Health, Is.Null, "Health should be null when no health provider");
    }

    [Test]
    public async Task SyncStatus_WithExternallyManagedSettings_ShouldIncludeThemInRequest()
    {
        // Arrange
        var externallyManagedSettings = new List<ExternallyManagedSettingDataContract>
        {
            new("TestSetting1", "overridden-value-1"),
            new("TestSetting2", 42)
        };
        ExternallyManagedSettingsBridge.ExternallyManagedSettings = externallyManagedSettings;

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(new StatusResponseDataContract
            {
                PollIntervalMs = 5000,
                SettingUpdateAvailable = false,
                AllowOfflineSettings = true,
                RestartRequested = false
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        _statusMonitor = new SettingStatusMonitor(
            _ipAddressResolverMock.Object,
            _versionProviderMock.Object,
            _diagnosticsMock.Object,
            _httpClientFactoryMock.Object,
            _configMock.Object,
            _clientSecretProviderMock.Object,
            _loggerMock.Object);

        // Act
        await _statusMonitor.SyncStatus();

        // Assert
        var httpRequestMessage = GetCapturedHttpRequest();
        var requestContent = await httpRequestMessage!.Content!.ReadAsStringAsync();
        var statusRequest = JsonConvert.DeserializeObject<StatusRequestDataContract>(requestContent);

        Assert.That(statusRequest!.ExternallyManagedSettings, Is.Not.Null, 
            "Should have externally managed settings in request");
        Assert.That(statusRequest.ExternallyManagedSettings!.Count, Is.EqualTo(2), 
            "Should have 2 externally managed settings");
        Assert.That(statusRequest.ExternallyManagedSettings[0].Name, Is.EqualTo("TestSetting1"));
        Assert.That(statusRequest.ExternallyManagedSettings[1].Name, Is.EqualTo("TestSetting2"));
    }

    [Test]
    public async Task SyncStatus_ExternallyManagedSettings_ShouldOnlyBeSentOnce()
    {
        // Arrange
        var externallyManagedSettings = new List<ExternallyManagedSettingDataContract>
        {
            new("TestSetting1", "overridden-value-1")
        };
        ExternallyManagedSettingsBridge.ExternallyManagedSettings = externallyManagedSettings;

        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(JsonConvert.SerializeObject(new StatusResponseDataContract
            {
                PollIntervalMs = 5000,
                SettingUpdateAvailable = false,
                AllowOfflineSettings = true,
                RestartRequested = false
            }))
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        _statusMonitor = new SettingStatusMonitor(
            _ipAddressResolverMock.Object,
            _versionProviderMock.Object,
            _diagnosticsMock.Object,
            _httpClientFactoryMock.Object,
            _configMock.Object,
            _clientSecretProviderMock.Object,
            _loggerMock.Object);

        // Act - first sync
        await _statusMonitor.SyncStatus();
        
        // Get and verify first request
        var firstRequest = GetCapturedHttpRequest();
        var firstContent = await firstRequest!.Content!.ReadAsStringAsync();
        var firstStatusRequest = JsonConvert.DeserializeObject<StatusRequestDataContract>(firstContent);
        
        Assert.That(firstStatusRequest!.ExternallyManagedSettings, Is.Not.Null, 
            "First request should have externally managed settings");
        Assert.That(firstStatusRequest.ExternallyManagedSettings!.Count, Is.EqualTo(1));

        // Clear invocations for second call
        _httpMessageHandlerMock.Invocations.Clear();

        // Act - second sync
        await _statusMonitor.SyncStatus();

        // Assert - second request should not have externally managed settings
        var secondRequest = GetCapturedHttpRequest();
        var secondContent = await secondRequest!.Content!.ReadAsStringAsync();
        var secondStatusRequest = JsonConvert.DeserializeObject<StatusRequestDataContract>(secondContent);

        Assert.That(secondStatusRequest!.ExternallyManagedSettings, Is.Null, 
            "Second request should not have externally managed settings (already sent)");
    }
    
    private HttpRequestMessage? GetCapturedHttpRequest()
    {
        // Get the captured HTTP request from the mock
        var invocations = _httpMessageHandlerMock.Invocations
            .Where(i => i.Method.Name == "SendAsync")
            .ToList();
        
        return invocations.LastOrDefault()?.Arguments[0] as HttpRequestMessage;
    }
}
