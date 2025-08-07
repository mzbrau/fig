using System;
using System.Linq;
using System.Threading.Tasks;
using Fig.Contracts.Health;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fig.Integration.Test.Api;

public class ClientRegistrationHealthTests : IntegrationTestBase
{
    [Test]
    public async Task ShallReportHealthStatusFromClientToApi()
    {
        // Arrange - Allow registrations
        await SetConfiguration(CreateConfiguration(allowNewRegistrations: true));
        
        var secret = GetNewSecret();
        
        // Create a test health report with multiple components
        var healthReport = new HealthDataContract
        {
            Status = FigHealthStatus.Healthy,
            Components = 
            [
                new("TestComponent", FigHealthStatus.Healthy, "All good"),
                new("DatabaseConnection", FigHealthStatus.Healthy, "Database responsive")
            ]
        };
        
        // Act - Initialize configuration provider with health monitoring
        var (_, config) = InitializeConfigurationProviderWithHealth<ThreeSettings>(secret, healthReport);
        
        // Wait for successful registration and health status reporting
        await WaitForCondition(
            async () =>
            {
                var statuses = (await GetAllStatuses()).ToList();
                if (!statuses.Any()) return false;
                
                var status = statuses.First();
                if (!status.RunSessions.Any()) return false;
                
                var runSession = status.RunSessions.First();
                return runSession.Health is { Status: FigHealthStatus.Healthy, Components.Count: >= 2 };
            }, 
            TimeSpan.FromSeconds(15),
            () => "Health status should be reported to API with all components");
        
        // Assert - Verify the health status was properly transmitted to the API
        var statuses = (await GetAllStatuses()).ToList();
        
        Assert.That(statuses.Count, Is.EqualTo(1), "Should have one client status");
        
        var clientStatus = statuses.Single();
        Assert.That(clientStatus.RunSessions.Count, Is.EqualTo(1), "Should have one run session");
        
        var runSession = clientStatus.RunSessions.Single();
        
        // Verify the health status was correctly transmitted
        Assert.That(runSession.Health, Is.Not.Null, "Health report should be present");
        Assert.That(runSession.Health!.Status, Is.EqualTo(FigHealthStatus.Healthy), 
            "Health status should be Healthy");
        
        // Verify the health components were transmitted
        Assert.That(runSession.Health.Components.Count, Is.GreaterThanOrEqualTo(2), 
            "Should have at least the 2 test components");
        
        var testComponent = runSession.Health.Components.FirstOrDefault(c => c.Name == "TestComponent");
        Assert.That(testComponent, Is.Not.Null, "Should have TestComponent");
        Assert.That(testComponent!.Status, Is.EqualTo(FigHealthStatus.Healthy));
        Assert.That(testComponent.Message, Is.EqualTo("All good"));
        
        var dbComponent = runSession.Health.Components.FirstOrDefault(c => c.Name == "DatabaseConnection");
        Assert.That(dbComponent, Is.Not.Null, "Should have DatabaseConnection component");
        Assert.That(dbComponent!.Status, Is.EqualTo(FigHealthStatus.Healthy));
        Assert.That(dbComponent.Message, Is.EqualTo("Database responsive"));
        
        (config as IDisposable)?.Dispose();
    }
    
    [Test]
    public async Task ShallMaintainOriginalHealthyStatusWhenRegistrationSucceeds()
    {
        // Arrange - Set configuration to allow new registrations
        await SetConfiguration(CreateConfiguration(allowNewRegistrations: true));
        
        var secret = GetNewSecret();
        
        // Create a test health report with multiple healthy components
        var healthReport = new HealthDataContract
        {
            Status = FigHealthStatus.Healthy,
            Components =
            [
                new("DatabaseConnection", FigHealthStatus.Healthy, "Database is responsive"),
                new("ExternalService", FigHealthStatus.Healthy, "Service is available")
            ]
        };
        
        // Act - Initialize a configuration provider with a client that should succeed registration
        var (_, config) = InitializeConfigurationProviderWithHealth<ThreeSettings>(secret, healthReport);
        
        // Wait for the registration and status monitoring to complete
        await WaitForCondition(
            async () =>
            {
                var statuses = (await GetAllStatuses()).ToList();
                return statuses.Any() && 
                       statuses.First().RunSessions.Any() && 
                       statuses.First().RunSessions.First().Health != null;
            }, 
            TimeSpan.FromSeconds(10),
            () => "Health status should be available after successful registration");
        
        // Assert - Check that the status maintains healthy status
        var statuses = (await GetAllStatuses()).ToList();
        
        Assert.That(statuses.Count, Is.EqualTo(1), "Should have one client status");
        
        var clientStatus = statuses.Single();
        Assert.That(clientStatus.RunSessions.Count, Is.EqualTo(1), "Should have one run session");
        
        var runSession = clientStatus.RunSessions.Single();
        
        // Verify the health status remains Healthy when registration succeeds
        Assert.That(runSession.Health, Is.Not.Null, "Health report should be present");
        Assert.That(runSession.Health!.Status, Is.EqualTo(FigHealthStatus.Healthy), 
            "Health status should remain Healthy when registration succeeds");
        
        // Verify the health report does not contain registration failure information
        var registrationComponent = runSession.Health.Components.FirstOrDefault(c => c.Name == "Registration");
        Assert.That(registrationComponent, Is.Null, 
            "Should not have a Registration component when registration succeeds");
        
        (config as IDisposable)?.Dispose();
    }
    
    private (IOptionsMonitor<T> options, IConfigurationRoot config) InitializeConfigurationProviderWithHealth<T>(
        string clientSecret, HealthDataContract healthReport) where T : TestSettingsBase
    {
        // This method simulates initializing a configuration provider with a health check
        // In a real scenario, the health check would be provided by the application
        // For testing purposes, we'll set up the health bridge to return our test health report
        Fig.Client.Health.HealthCheckBridge.GetHealthReportAsync = () => Task.FromResult(healthReport);
        
        return InitializeConfigurationProvider<T>(clientSecret);
    }
}
