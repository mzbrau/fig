using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Client.ExtensionMethods;
using Fig.Client.Enums;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

[TestFixture]
public class SettingLoadTypeAndTimestampTests : IntegrationTestBase
{
    private string? _originalPollInterval;
    private static readonly TimeSpan TimestampTolerance = TimeSpan.FromSeconds(30); // More generous tolerance for slow environments
    
    [OneTimeSetUp]
    public void OneTimeSetUp()
    {
        _originalPollInterval = Environment.GetEnvironmentVariable("FIG_POLL_INTERVAL_MS");
        Environment.SetEnvironmentVariable("FIG_POLL_INTERVAL_MS", "1000"); // 1 second for tests
    }
    
    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        Environment.SetEnvironmentVariable("FIG_POLL_INTERVAL_MS", _originalPollInterval);
    }
    [Test]
    public async Task ShallHaveValidTimestampAndServerLoadTypeWhenLoadingFromApi()
    {
        // Arrange
        var secret = GetNewSecret();
        await RegisterSettings<AllSettingsAndTypes>(secret);
        var beforeLoadTime = DateTime.UtcNow;
        
        // Act
        var (options, _) = InitializeConfigurationProvider<AllSettingsAndTypes>(secret);

        // Assert
        var currentSettings = options.CurrentValue;
        Assert.That(currentSettings.FigSettingLoadType, Is.EqualTo(LoadType.Server), 
            "LoadType should be Server when successfully loading from API");
        
        Assert.That(currentSettings.LastFigUpdateUtc, Is.Not.Null, 
            "LastFigUpdateUtc should not be null when loading from API");
        
        var lastUpdateTime = currentSettings.LastFigUpdateUtc!.Value;
        AssertTimestampIsRecent(lastUpdateTime, beforeLoadTime, "when loading from API");
    }

    [Test]
    public void ShallHaveNoneLoadTypeAndNullTimestampWhenApiIsUnavailableAndNoOfflineSettings()
    {
        // Act
        var (options, _) = InitializeConfigurationProviderWithInvalidEndpoint<AllSettingsAndTypes>(
            clientSecret: GetNewSecret(), 
            allowOfflineSettings: false);

        // Assert
        var currentSettings = options.CurrentValue;
        Assert.That(currentSettings.FigSettingLoadType, Is.EqualTo(LoadType.None), 
            "LoadType should be None when API is unavailable and offline settings are disabled");
        
        Assert.That(currentSettings.LastFigUpdateUtc, Is.Null, 
            "LastFigUpdateUtc should be null when settings fail to load completely");
    }

    [Test]
    public async Task ShallHaveOfflineLoadTypeAndValidTimestampWhenUsingOfflineSettings()
    {
        // Arrange - First, create valid settings and load them to create offline cache
        var secret = GetNewSecret();
        await RegisterSettings<AllSettingsAndTypes>(secret);
        
        // Load settings once to create offline cache
        var (initialOptions, initialConfig) = InitializeConfigurationProvider<AllSettingsAndTypes>(secret);
        await WaitForSettingsToLoad(initialOptions);
        
        // Dispose the initial configuration to clean up
        (initialConfig as IDisposable)?.Dispose();
        
        var beforeOfflineLoadTime = DateTime.UtcNow;
        
        // Act - Configure with invalid endpoint but allow offline settings
        var (offlineOptions, _) = InitializeConfigurationProviderWithInvalidEndpoint<AllSettingsAndTypes>(
            clientSecret: secret, 
            allowOfflineSettings: true);

        // Assert
        var currentSettings = offlineOptions.CurrentValue;
        Assert.That(currentSettings.FigSettingLoadType, Is.EqualTo(LoadType.Offline), 
            "LoadType should be Offline when API is unavailable but offline settings are available");
        
        Assert.That(currentSettings.LastFigUpdateUtc, Is.Not.Null, 
            "LastFigUpdateUtc should not be null when loading from offline settings");
        
        var lastUpdateTime = currentSettings.LastFigUpdateUtc!.Value;
        AssertTimestampIsRecent(lastUpdateTime, beforeOfflineLoadTime, "when loading from offline settings");
    }

    [Test]
    public async Task ShallUpdateTimestampWhenSettingsReload()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<AllSettingsAndTypes>(secret);
        var (options, _) = InitializeConfigurationProvider<AllSettingsAndTypes>(secret);

        // Get initial timestamp
        var initialTimestamp = options.CurrentValue.LastFigUpdateUtc;
        Assert.That(initialTimestamp, Is.Not.Null);

        // Wait a small amount to ensure timestamp difference
        await Task.Delay(100);

        // Act - Update a setting to trigger a reload
        await UpdateStringSetting(settings.ClientName, "UpdatedValue");
        
        // Wait for the reload to complete
        await WaitForSettingsUpdate(options, initialTimestamp!.Value);

        // Assert
        var updatedSettings = options.CurrentValue;
        Assert.That(updatedSettings.FigSettingLoadType, Is.EqualTo(LoadType.Server), 
            "LoadType should remain Server after reload");
        
        Assert.That(updatedSettings.LastFigUpdateUtc, Is.Not.Null, 
            "LastFigUpdateUtc should not be null after reload");
        
        Assert.That(updatedSettings.LastFigUpdateUtc!.Value, Is.GreaterThan(initialTimestamp.Value), 
            "LastFigUpdateUtc should be updated after settings reload");
        
        AssertTimestampIsRecent(updatedSettings.LastFigUpdateUtc.Value, initialTimestamp.Value, "after settings reload");
    }

    [Test]
    public async Task ShallMaintainLoadTypeConsistencyAcrossMultipleReloads()
    {
        // Arrange
        var secret = GetNewSecret();
        var settings = await RegisterSettings<AllSettingsAndTypes>(secret);
        var (options, _) = InitializeConfigurationProvider<AllSettingsAndTypes>(secret);

        // Act & Assert - Perform multiple updates
        var previousTimestamp = options.CurrentValue.LastFigUpdateUtc!.Value;
        
        for (int i = 0; i < 3; i++)
        {
            await Task.Delay(100); // Ensure timestamp difference
            
            await UpdateStringSetting(settings.ClientName, $"UpdatedValue_{i}");
            await WaitForSettingsUpdate(options, previousTimestamp);

            var currentSettings = options.CurrentValue;
            Assert.That(currentSettings.FigSettingLoadType, Is.EqualTo(LoadType.Server), 
                $"LoadType should remain Server after reload {i + 1}");
            
            Assert.That(currentSettings.LastFigUpdateUtc, Is.Not.Null, 
                $"LastFigUpdateUtc should not be null after reload {i + 1}");
            
            Assert.That(currentSettings.LastFigUpdateUtc!.Value, Is.GreaterThan(previousTimestamp), 
                $"LastFigUpdateUtc should be updated after reload {i + 1}");
            
            AssertTimestampIsRecent(currentSettings.LastFigUpdateUtc.Value, previousTimestamp, $"after reload {i + 1}");

            previousTimestamp = currentSettings.LastFigUpdateUtc.Value;
        }
    }

    private (IOptionsMonitor<T>, IConfigurationRoot) InitializeConfigurationProviderWithInvalidEndpoint<T>(
        string clientSecret, 
        bool allowOfflineSettings = true) where T : TestSettingsBase
    {
        var builder = WebApplication.CreateBuilder();
        var settings = Activator.CreateInstance<T>();

        var configuration = new ConfigurationBuilder()
            .AddFig<T>(o =>
            {
                o.ClientName = settings.ClientName;
                o.HttpClient = new HttpClient()
                {
                    BaseAddress = new Uri("http://localhost:9999") // Invalid endpoint
                };
                o.ClientSecretOverride = clientSecret;
                o.AllowOfflineSettings = allowOfflineSettings;
            }).Build();

        builder.Services.Configure<T>(configuration);
        var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptionsMonitor<T>>();
        ConfigProviderApps.Add(app);
        ConfigRoots.Add(configuration);
        return (options, configuration);
    }

    private async Task WaitForSettingsToLoad<T>(IOptionsMonitor<T> options, int timeoutMs = 5000) where T : TestSettingsBase
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < timeoutMs)
        {
            if (options.CurrentValue.LastFigUpdateUtc != null)
                return;
            
            await Task.Delay(50);
        }
        
        throw new TimeoutException("Settings did not load within the expected time");
    }

    private async Task WaitForSettingsUpdate<T>(IOptionsMonitor<T> options, DateTime previousTimestamp, int timeoutMs = 5000) where T : TestSettingsBase
    {
        var startTime = DateTime.UtcNow;
        while (DateTime.UtcNow.Subtract(startTime).TotalMilliseconds < timeoutMs)
        {
            var currentTimestamp = options.CurrentValue.LastFigUpdateUtc;
            if (currentTimestamp != null && currentTimestamp.Value > previousTimestamp)
                return;
            
            await Task.Delay(50);
        }
        
        throw new TimeoutException("Settings were not updated within the expected time");
    }

    private static void AssertTimestampIsRecent(DateTime timestamp, DateTime earliestExpected, string context)
    {
        Assert.That(timestamp, Is.GreaterThanOrEqualTo(earliestExpected), 
            $"LastFigUpdateUtc should be after the expected minimum time {context}");
        
        Assert.That(timestamp, Is.LessThanOrEqualTo(DateTime.UtcNow.Add(TimestampTolerance)), 
            $"LastFigUpdateUtc should be within tolerance of current time {context} (tolerance: {TimestampTolerance})");
        
        Assert.That(timestamp.Kind, Is.EqualTo(DateTimeKind.Utc), 
            $"LastFigUpdateUtc should be in UTC {context}");
    }

    private async Task UpdateStringSetting(string clientName, string newValue)
    {
        var settingsToUpdate = new List<SettingDataContract>
        {
            new(nameof(AllSettingsAndTypes.StringSetting), new StringSettingDataContract(newValue))
        };

        await SetSettings(clientName, settingsToUpdate);
    }
}
