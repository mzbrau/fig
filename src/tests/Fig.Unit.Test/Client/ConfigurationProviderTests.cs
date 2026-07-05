using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Fig.Common.NetStandard.IpAddress;
using System.Net.Http;
using Fig.Client;
using Fig.Contracts;
using Fig.Contracts.Settings;
using Moq;
using Fig.Client.Status;
using System;
using System.Linq;
using Fig.Client.Abstractions.Attributes;
using Fig.Client.ClientSecret;
using Fig.Contracts.SettingDefinitions;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Contracts;
using Fig.Client.Exceptions;
using Fig.Client.OfflineSettings;
using Fig.Client.RegistrationChecksum;
using Fig.Unit.Test.TestInfrastructure;
using Microsoft.Extensions.Logging;
using Fig.Contracts.SettingMigrations;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ConfigurationProviderTests
{
    private readonly Mock<IApiCommunicationHandler> _apiCommunicationHandlerMock = new();
    private readonly Mock<ISettingStatusMonitor> _settingStatusMonitorMock = new();

    [SetUp]
    public void SetUp()
    {
        _apiCommunicationHandlerMock.Reset();
        _settingStatusMonitorMock.Reset();
        RunSession.Clear();
        RegisteredProviders.Clear();
    }

    [TearDown]
    public void TearDown()
    {
        RunSession.Clear();
        FigClientBridgeRegistry.Clear();
        RegisteredProviders.Clear();
    }

    [Test]
    public void ShallSetConfigurationCorrectly()
    {
        var source = CreateSource();

        var result = MockApiResponse(source);

        var builder = new ConfigurationBuilder();
        builder.Add(source);
        var configuration = builder.Build();

        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.Configure<AllSettingsAndTypes>(configuration).BuildServiceProvider();
        var clientOptions = serviceProvider.GetRequiredService<IOptions<AllSettingsAndTypes>>().Value;

        var expectedPropertyValues = result.Where(a => a.Value is not DataGridSettingDataContract).ToDictionary(a => a.Name, b => GetValue(b.Value!));

        foreach (var (propertyName, expectedValue) in expectedPropertyValues)
        {
            var iOptionsValue = GetPropertyValue(clientOptions, propertyName);

            Assert.That(expectedValue, Is.EqualTo(iOptionsValue), $"IOptions value for {propertyName} should be set correctly");
        }

        AssertStringCollectionWasCorrect(clientOptions);
        AssertObjectListWasCorrect(clientOptions);
    }

    [Test]
    public void ShallRegisterSettingsWithApi()
    {
        var source = CreateSource();
        _apiCommunicationHandlerMock.Setup(a => a.RequestConfiguration()).ReturnsAsync([]);

        var builder = new ConfigurationBuilder();
        builder.Add(source);
        builder.Build();

        _apiCommunicationHandlerMock.Verify(a => a.RegisterWithFigApi(It.Is<SettingsClientDefinitionDataContract>(definition => VerifyDefinition(definition))));
    }

    [Test]
    public void ShallRegisterSettingsWhenMigrateFromPreviewReturnsNull()
    {
        var source = CreateSource();
        source.SettingsType = typeof(SettingsWithPreviewMigration);
        _apiCommunicationHandlerMock
            .Setup(a => a.GetMigrateFromMigrationRequests(It.IsAny<SettingsClientDefinitionDataContract>()))
            .Returns(Task.FromResult<List<SettingMigrationRequestDataContract>>(null!));
        _apiCommunicationHandlerMock
            .Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        _apiCommunicationHandlerMock
            .Setup(a => a.RequestConfiguration())
            .ReturnsAsync([]);

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        Assert.DoesNotThrow(() => builder.Build());
        _apiCommunicationHandlerMock.Verify(
            a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()),
            Times.Once);
    }

    [Test]
    public void ShallRegisterSettingsWhenMigrateFromPreviewFailsWithTransportError()
    {
        var source = CreateSource();
        source.SettingsType = typeof(SettingsWithPreviewMigration);
        _apiCommunicationHandlerMock
            .Setup(a => a.GetMigrateFromMigrationRequests(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ThrowsAsync(new HttpRequestException("Preview failed"));
        _apiCommunicationHandlerMock
            .Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        _apiCommunicationHandlerMock
            .Setup(a => a.RequestConfiguration())
            .ReturnsAsync([]);

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        Assert.DoesNotThrow(() => builder.Build());
        _apiCommunicationHandlerMock.Verify(
            a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()),
            Times.Once);
    }

    [Test]
    public void ShallRegisterSettingsWhenMigrateFromPreviewFailsWithCompatibilityError()
    {
        var source = CreateSource();
        source.SettingsType = typeof(SettingsWithPreviewMigration);
        _apiCommunicationHandlerMock
            .Setup(a => a.GetMigrateFromMigrationRequests(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ThrowsAsync(new FigRegistrationException(null));
        _apiCommunicationHandlerMock
            .Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        _apiCommunicationHandlerMock
            .Setup(a => a.RequestConfiguration())
            .ReturnsAsync([]);

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        Assert.DoesNotThrow(() => builder.Build());
        _apiCommunicationHandlerMock.Verify(
            a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()),
            Times.Once);
    }

    [Test]
    public void ShallFailFastWhenLocalMigrateFromConversionFails()
    {
        var source = CreateSource();
        source.SettingsType = typeof(SettingsWithThrowingPreviewMigration);
        _apiCommunicationHandlerMock
            .Setup(a => a.GetMigrateFromMigrationRequests(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync([
                new SettingMigrationRequestDataContract(
                    "OldSetting",
                    "NewSetting",
                    null,
                    typeof(string),
                    typeof(string),
                    new StringSettingDataContract("legacy"),
                    false,
                    false,
                    "fingerprint")
            ]);
        _apiCommunicationHandlerMock
            .Setup(a => a.RequestConfiguration())
            .ReturnsAsync([]);

        var builder = new ConfigurationBuilder();
        builder.Add(source);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.That(ex!.Message, Does.Contain("Local migration failed"));
        _apiCommunicationHandlerMock.Verify(
            a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()),
            Times.Never);
    }

    [Test]
    public void Dispose_ReleasesRunSession()
    {
        var source = CreateSource();

        var configuration = new ConfigurationBuilder()
            .Add(source)
            .Build();

        Assert.That(RunSession.Count, Is.EqualTo(1));

        ((IDisposable)configuration).Dispose();

        Assert.That(RunSession.Count, Is.EqualTo(0));
    }

    [Test]
    public void Build_WithSameRegistrationKey_ReusesExistingProvider()
    {
        var source = CreateSource();

        var firstProvider = source.Build(new ConfigurationBuilder());
        var secondProvider = source.Build(new ConfigurationBuilder());

        Assert.That(secondProvider, Is.SameAs(firstProvider));
        Assert.That(RegisteredProviders.Count, Is.EqualTo(1));
    }

    [Test]
    public void Build_WithDifferentInstance_DoesNotReuseExistingProvider()
    {
        var firstSource = CreateSource();
        var secondSource = CreateSource();
        secondSource.Instance = "secondary";

        var firstProvider = firstSource.Build(new ConfigurationBuilder());
        var secondProvider = secondSource.Build(new ConfigurationBuilder());

        Assert.That(secondProvider, Is.Not.SameAs(firstProvider));
        Assert.That(RegisteredProviders.Count, Is.EqualTo(2));
    }

    [Test]
    public void Build_WithEmptyInstance_ReusesNullInstanceProvider()
    {
        var firstSource = CreateSource();
        var secondSource = CreateSource();
        secondSource.Instance = string.Empty;

        var firstProvider = firstSource.Build(new ConfigurationBuilder());
        var secondProvider = secondSource.Build(new ConfigurationBuilder());

        Assert.That(secondProvider, Is.SameAs(firstProvider));
        Assert.That(RegisteredProviders.Count, Is.EqualTo(1));
    }

    [Test]
    public void Build_WithDifferentSettingsType_DoesNotReuseExistingProvider()
    {
        var firstSource = CreateSource();
        var secondSource = CreateSource();
        secondSource.SettingsType = typeof(SimpleSettings);

        var firstProvider = firstSource.Build(new ConfigurationBuilder());
        var secondProvider = secondSource.Build(new ConfigurationBuilder());

        Assert.That(secondProvider, Is.Not.SameAs(firstProvider));
        Assert.That(RegisteredProviders.Count, Is.EqualTo(2));
    }

    [Test]
    public void Dispose_UnregistersProvider()
    {
        var source = CreateSource();

        var provider = source.Build(new ConfigurationBuilder());
        var foundBeforeDispose = RegisteredProviders.TryGet(
            source.ClientName,
            source.Instance,
            source.SettingsType,
            out var registeredProvider);

        ((IDisposable)provider).Dispose();
        var foundAfterDispose = RegisteredProviders.TryGet(
            source.ClientName,
            source.Instance,
            source.SettingsType,
            out _);

        Assert.That(foundBeforeDispose, Is.True);
        Assert.That(registeredProvider, Is.SameAs(provider));
        Assert.That(foundAfterDispose, Is.False);
        Assert.That(RegisteredProviders.Count, Is.EqualTo(0));
    }

    [Test]
    public void TryGet_WhenProviderIsDisposedAndStillReachable_PrunesProvider()
    {
        var source = CreateSource();
        var provider = source.Build(new ConfigurationBuilder());

        ((IDisposable)provider).Dispose();
        RegisteredProviders.Register((FigConfigurationProvider)provider);

        var found = RegisteredProviders.TryGet(
            source.ClientName,
            source.Instance,
            source.SettingsType,
            out _);

        Assert.That(found, Is.False);
        Assert.That(RegisteredProviders.Count, Is.EqualTo(0));
    }

    [Test]
    public void TryGet_ByName_WithMultipleProvidersForSameClientName_ReturnsFalse()
    {
        var firstSource = CreateSource();
        var secondSource = CreateSource();
        secondSource.Instance = "secondary";

        firstSource.Build(new ConfigurationBuilder());
        secondSource.Build(new ConfigurationBuilder());

        var found = RegisteredProviders.TryGet(firstSource.ClientName, out var provider);

        Assert.That(found, Is.False);
        Assert.That(provider, Is.Null);
    }

    [Test]
    public void Dispose_UnregistersClientBridge()
    {
        var apiHandlerMock = new Mock<IApiCommunicationHandler>();
        apiHandlerMock.As<IFigClientBridge>();
        apiHandlerMock
            .Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);

        var source = new TestableConfigurationSource(apiHandlerMock, _settingStatusMonitorMock)
        {
            ApiUris = ["x"],
            PollIntervalMs = 30000,
            LiveReload = false,
            Instance = null,
            ClientName = "test",
            AllowOfflineSettings = false,
            SettingsType = typeof(AllSettingsAndTypes),
            ClientSecretProviders = [new InCodeClientSecretProvider(Mock.Of<ILogger<InCodeClientSecretProvider>>(), Guid.NewGuid().ToString())]
        };

        var configuration = new ConfigurationBuilder()
            .Add(source)
            .Build();

        var foundBeforeDispose = FigClientBridgeRegistry.TryGet(typeof(AllSettingsAndTypes), out var bridge, out _);

        ((IDisposable)configuration).Dispose();

        var foundAfterDispose = FigClientBridgeRegistry.TryGet(typeof(AllSettingsAndTypes), out _, out _);

        Assert.That(foundBeforeDispose, Is.True);
        Assert.That(bridge, Is.Not.Null);
        Assert.That(bridge, Is.Not.SameAs(apiHandlerMock.Object));
        Assert.That(foundAfterDispose, Is.False);
    }

    [Test]
    public async Task ApplyAsync_RefreshesConfigurationBeforeReturning()
    {
        var apiHandlerMock = new Mock<IApiCommunicationHandler>();
        var bridgeMock = apiHandlerMock.As<IFigClientBridge>();
        apiHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        apiHandlerMock.SetupSequence(a => a.RequestConfiguration())
            .ReturnsAsync([CreateStringSetting(nameof(AllSettingsAndTypes.StringSetting), "initial")])
            .ReturnsAsync([CreateStringSetting(nameof(AllSettingsAndTypes.StringSetting), "updated")]);
        bridgeMock.Setup(a => a.UpdateSettings(It.IsAny<SettingValueUpdatesDataContract>()))
            .Returns(Task.CompletedTask);

        var source = CreateSource(apiHandlerMock, _settingStatusMonitorMock);
        var configuration = new ConfigurationBuilder()
            .Add(source)
            .Build();
        var serviceProvider = new ServiceCollection()
            .Configure<AllSettingsAndTypes>(configuration)
            .BuildServiceProvider();
        var updater = new SettingUpdater<AllSettingsAndTypes>();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<AllSettingsAndTypes>>();

        Assert.That(optionsMonitor.CurrentValue.StringSetting, Is.EqualTo("initial"));

        await updater
            .Set(a => a.StringSetting, "updated")
            .ApplyAsync();

        Assert.That(configuration[nameof(AllSettingsAndTypes.StringSetting)], Is.EqualTo("updated"));
        Assert.That(optionsMonitor.CurrentValue.StringSetting, Is.EqualTo("updated"));
        apiHandlerMock.Verify(a => a.RequestConfiguration(), Times.Exactly(2));
    }

    [Test]
    public void ApplyAsync_WhenRemoteUpdateFails_DoesNotReloadConfiguration()
    {
        var apiHandlerMock = new Mock<IApiCommunicationHandler>();
        var bridgeMock = apiHandlerMock.As<IFigClientBridge>();
        apiHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        apiHandlerMock.Setup(a => a.RequestConfiguration())
            .ReturnsAsync([CreateStringSetting(nameof(AllSettingsAndTypes.StringSetting), "initial")]);
        bridgeMock.Setup(a => a.UpdateSettings(It.IsAny<SettingValueUpdatesDataContract>()))
            .ThrowsAsync(new FigSettingUpdateException(new ErrorResultDataContract("update_failed", "Update failed", null, null)));

        var source = CreateSource(apiHandlerMock, _settingStatusMonitorMock);
        _ = new ConfigurationBuilder()
            .Add(source)
            .Build();
        var updater = new SettingUpdater<AllSettingsAndTypes>();

        Assert.ThrowsAsync<FigSettingUpdateException>(async () =>
            await updater.Set(a => a.StringSetting, "updated").ApplyAsync());
        apiHandlerMock.Verify(a => a.RequestConfiguration(), Times.Once);
    }

    [Test]
    public async Task ApplyAsync_WhenRefreshFails_ThrowsDistinctExceptionAndKeepsQueuedUpdate()
    {
        var apiHandlerMock = new Mock<IApiCommunicationHandler>();
        var bridgeMock = apiHandlerMock.As<IFigClientBridge>();
        apiHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        apiHandlerMock.SetupSequence(a => a.RequestConfiguration())
            .ReturnsAsync([CreateStringSetting(nameof(AllSettingsAndTypes.StringSetting), "initial")])
            .ThrowsAsync(new HttpRequestException("refresh failed"))
            .ReturnsAsync([CreateStringSetting(nameof(AllSettingsAndTypes.StringSetting), "updated")]);
        bridgeMock.Setup(a => a.UpdateSettings(It.IsAny<SettingValueUpdatesDataContract>()))
            .Returns(Task.CompletedTask);

        var source = CreateSource(apiHandlerMock, _settingStatusMonitorMock);
        var configuration = new ConfigurationBuilder()
            .Add(source)
            .Build();
        var serviceProvider = new ServiceCollection()
            .Configure<AllSettingsAndTypes>(configuration)
            .BuildServiceProvider();
        var updater = new SettingUpdater<AllSettingsAndTypes>();
        var optionsMonitor = serviceProvider.GetRequiredService<IOptionsMonitor<AllSettingsAndTypes>>();

        var exception = Assert.ThrowsAsync<FigSettingRefreshException>(async () =>
            await updater.Set(a => a.StringSetting, "updated").ApplyAsync());

        Assert.That(exception!.InnerException, Is.TypeOf<HttpRequestException>());
        Assert.That(optionsMonitor.CurrentValue.StringSetting, Is.EqualTo("initial"));

        await updater.ApplyAsync();

        Assert.That(optionsMonitor.CurrentValue.StringSetting, Is.EqualTo("updated"));
        bridgeMock.Verify(a => a.UpdateSettings(It.IsAny<SettingValueUpdatesDataContract>()), Times.Exactly(2));
        apiHandlerMock.Verify(a => a.RequestConfiguration(), Times.Exactly(3));
    }

    [Test]
    public void ClientBridgeRegistry_RetainsProviderBackedBridgeWhileProviderIsAlive()
    {
        var apiHandlerMock = new Mock<IApiCommunicationHandler>();
        apiHandlerMock.As<IFigClientBridge>();
        apiHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        apiHandlerMock.Setup(a => a.RequestConfiguration()).ReturnsAsync([]);

        var source = CreateSource(apiHandlerMock, _settingStatusMonitorMock);
        var configuration = new ConfigurationBuilder()
            .Add(source)
            .Build();

        try
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            var found = FigClientBridgeRegistry.TryGet(typeof(AllSettingsAndTypes), out var bridge, out _);

            Assert.That(found, Is.True);
            Assert.That(bridge, Is.Not.Null);
            Assert.That(bridge, Is.Not.SameAs(apiHandlerMock.Object));
        }
        finally
        {
            (configuration as IDisposable)?.Dispose();
        }
    }

    [Test]
    public void TryGetAndGetChildKeys_WaitForDataLockDuringReads()
    {
        var apiHandlerMock = new Mock<IApiCommunicationHandler>();
        apiHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        apiHandlerMock.Setup(a => a.RequestConfiguration())
            .ReturnsAsync([CreateStringSetting(nameof(AllSettingsAndTypes.StringSetting), "value")]);

        var source = CreateSource(apiHandlerMock, _settingStatusMonitorMock);
        var provider = CreateProvider(source, apiHandlerMock, _settingStatusMonitorMock);
        provider.Load();

        var dataLock = typeof(FigConfigurationProvider)
            .GetField("_dataLock", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(provider)!;

        Task<bool>? tryGetTask = null;
        Task<List<string>>? childKeysTask = null;
        using var readsReady = new CountdownEvent(2);
        provider.BeforeDataReadLockEnterForTesting = () => readsReady.Signal();

        try
        {
            Monitor.Enter(dataLock);
            tryGetTask = Task.Run(() => provider.TryGet(nameof(AllSettingsAndTypes.StringSetting), out var value) && value == "value");
            childKeysTask = Task.Run(() => provider.GetChildKeys([], null).ToList());

            Assert.That(readsReady.Wait(TimeSpan.FromSeconds(1)), Is.True);
            Assert.That(tryGetTask.Wait(TimeSpan.FromMilliseconds(100)), Is.False);
            Assert.That(childKeysTask.Wait(TimeSpan.FromMilliseconds(100)), Is.False);
        }
        finally
        {
            provider.BeforeDataReadLockEnterForTesting = null;
            Monitor.Exit(dataLock);
        }

        try
        {
            Assert.That(tryGetTask!.Result, Is.True);
            Assert.That(childKeysTask!.Result, Does.Contain(nameof(AllSettingsAndTypes.StringSetting)));
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Test]
    public void Load_WhenOfflineSaveThrows_UsesInMemorySnapshotAndLogsWarning()
    {
        var apiHandlerMock = new Mock<IApiCommunicationHandler>();
        apiHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        apiHandlerMock.Setup(a => a.RequestConfiguration())
            .ReturnsAsync([CreateStringSetting(nameof(AllSettingsAndTypes.StringSetting), "server-value")]);
        var offlineSettingsManagerMock = new Mock<IOfflineSettingsManager>();
        offlineSettingsManagerMock.Setup(a => a.Save(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<IEnumerable<SettingDataContract>>()))
            .ThrowsAsync(new IOException("disk full"));
        var statusMonitorMock = new Mock<ISettingStatusMonitor>();
        var loggerMock = new Mock<ILogger<FigConfigurationProvider>>();

        var source = CreateSource(apiHandlerMock, statusMonitorMock);
        source.AllowOfflineSettings = true;
        var provider = CreateProvider(source, apiHandlerMock, statusMonitorMock, offlineSettingsManagerMock.Object, loggerMock.Object);

        try
        {
            Assert.DoesNotThrow(() => provider.Load());
            Assert.That(provider.TryGet(nameof(AllSettingsAndTypes.StringSetting), out var value), Is.True);
            Assert.That(value, Is.EqualTo("server-value"));
            statusMonitorMock.Verify(a => a.SettingsUpdated(), Times.Once);
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("Failed to persist offline settings")),
                    It.Is<IOException>(ex => ex.Message == "disk full"),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Test]
    public void ShallHandleRestartRequest([Values] bool restartRequested)
    {
        var source = CreateSource();

        MockApiResponse(source);

        var builder = new ConfigurationBuilder();
        builder.Add(source);
        var configuration = builder.Build();

        if (restartRequested)
            _settingStatusMonitorMock.Raise(a => a.RestartRequested += null, EventArgs.Empty);

        var serviceCollection = new ServiceCollection();
        var serviceProvider = serviceCollection.Configure<AllSettingsAndTypes>(configuration).BuildServiceProvider();
        var clientOptions = serviceProvider.GetRequiredService<IOptions<AllSettingsAndTypes>>().Value;

        Assert.That(clientOptions.RestartRequested, Is.EqualTo(restartRequested));
    }
    
    [Test]
    public void ShallUseEnvironmentVariablesOverDefaults()
    {
        // Arrange - Set environment variables that should override defaults
        var testEnvVarValue = "TestValueFromEnvironment";
        var settingName = nameof(AllSettingsAndTypes.StringSetting);
        var envVarName = settingName;
        
        // Save original value for cleanup
        var originalValue = Environment.GetEnvironmentVariable(envVarName);
        
        try
        {
            Environment.SetEnvironmentVariable(envVarName, testEnvVarValue);

            // Create fresh mock for this test
            var apiHandlerMock = new Mock<IApiCommunicationHandler>();
            var statusMonitorMock = new Mock<ISettingStatusMonitor>();
            
            // Configure the API call to fail (simulate offline scenario)
            apiHandlerMock
                .Setup(a => a.RequestConfiguration())
                .ThrowsAsync(new HttpRequestException("API unavailable"));
            
            statusMonitorMock.Setup(a => a.Initialize());
            statusMonitorMock.Setup(a => a.AllowOfflineSettings).Returns(true);

            var source = new TestableConfigurationSource(apiHandlerMock, statusMonitorMock)
            {
                ApiUris = ["http://localhost:5000"],
                PollIntervalMs = 30000,
                LiveReload = false,
                Instance = null,
                ClientName = "envtest",
                AllowOfflineSettings = true,
                SettingsType = typeof(AllSettingsAndTypes),
                ClientSecretProviders = [new InCodeClientSecretProvider(Mock.Of<ILogger<InCodeClientSecretProvider>>(), Guid.NewGuid().ToString())]
            };

            var builder = new ConfigurationBuilder();
            builder.Add(source);
            var configuration = builder.Build();

            // Act - Read the configuration value
            var actualValue = configuration[settingName];

            // Assert - Environment variable should take precedence over Fig's default value
            Assert.That(actualValue, Is.EqualTo(testEnvVarValue), 
                "Environment variable should override Fig default value when API is unavailable");
        }
        finally 
        {
            // Cleanup - restore original value
            Environment.SetEnvironmentVariable(envVarName, originalValue);
        }
    }

    [Test]
    public void ShallSkipRegistration_WhenChecksumMatches()
    {
        var settings = new AllSettingsAndTypes();
        var checksum = RegistrationChecksumCalculator.Compute(settings.CreateDataContract("test", true));
        var checksumStoreMock = new Mock<IRegistrationChecksumStore>();
        checksumStoreMock.Setup(s => s.Get("test", null)).Returns(checksum);

        _apiCommunicationHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        _apiCommunicationHandlerMock.Setup(a => a.RequestConfiguration()).ReturnsAsync([]);

        var source = CreateSource();
        var provider = CreateProvider(source, _apiCommunicationHandlerMock, _settingStatusMonitorMock, registrationChecksumStore: checksumStoreMock.Object);

        try
        {
            provider.Load();
            _apiCommunicationHandlerMock.Verify(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()), Times.Never);
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Test]
    public void ShallRegisterAndSaveChecksum_WhenChecksumMissing()
    {
        var checksumStoreMock = new Mock<IRegistrationChecksumStore>();
        checksumStoreMock.Setup(s => s.Get("test", null)).Returns((string?)null);

        _apiCommunicationHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        _apiCommunicationHandlerMock.Setup(a => a.RequestConfiguration()).ReturnsAsync([]);

        var source = CreateSource();
        var provider = CreateProvider(source, _apiCommunicationHandlerMock, _settingStatusMonitorMock, registrationChecksumStore: checksumStoreMock.Object);

        try
        {
            provider.Load();
            _apiCommunicationHandlerMock.Verify(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()), Times.Once);
            checksumStoreMock.Verify(s => s.Save("test", null, It.IsAny<string>()), Times.Once);
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Test]
    public void ShallRegisterOnSettingsNotFound_WhenChecksumMatchedButClientDeletedOnServer()
    {
        var settings = new AllSettingsAndTypes();
        var checksum = RegistrationChecksumCalculator.Compute(settings.CreateDataContract("test", true));
        var checksumStoreMock = new Mock<IRegistrationChecksumStore>();
        checksumStoreMock.Setup(s => s.Get("test", null)).Returns(checksum);

        var requestCount = 0;
        _apiCommunicationHandlerMock.Setup(a => a.RequestConfiguration())
            .Returns(() =>
            {
                requestCount++;
                if (requestCount == 1)
                    throw new FigClientNotFoundException("test", null);
                return Task.FromResult<List<SettingDataContract>>([]);
            });
        _apiCommunicationHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);

        var source = CreateSource();
        var provider = CreateProvider(source, _apiCommunicationHandlerMock, _settingStatusMonitorMock, registrationChecksumStore: checksumStoreMock.Object);

        try
        {
            provider.Load();
            _apiCommunicationHandlerMock.Verify(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()), Times.Once);
            Assert.That(requestCount, Is.EqualTo(2));
        }
        finally
        {
            provider.Dispose();
        }
    }

    [Test]
    public void ShallRegister_WhenRegistrationChecksumDisabledEvenIfChecksumMatches()
    {
        var settings = new AllSettingsAndTypes();
        var checksum = RegistrationChecksumCalculator.Compute(settings.CreateDataContract("test", true));
        var checksumStoreMock = new Mock<IRegistrationChecksumStore>();
        checksumStoreMock.Setup(s => s.Get("test", null)).Returns(checksum);

        _apiCommunicationHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(true);
        _apiCommunicationHandlerMock.Setup(a => a.RequestConfiguration()).ReturnsAsync([]);

        var originalValue = Environment.GetEnvironmentVariable("FIG_DISABLE_REGISTRATION_CHECKSUM");
        Environment.SetEnvironmentVariable("FIG_DISABLE_REGISTRATION_CHECKSUM", "true");

        try
        {
            var source = CreateSource();
            var provider = CreateProvider(source, _apiCommunicationHandlerMock, _settingStatusMonitorMock, registrationChecksumStore: checksumStoreMock.Object);
            provider.Load();
            _apiCommunicationHandlerMock.Verify(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()), Times.Once);
            checksumStoreMock.Verify(s => s.Save(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
            provider.Dispose();
        }
        finally
        {
            Environment.SetEnvironmentVariable("FIG_DISABLE_REGISTRATION_CHECKSUM", originalValue);
        }
    }

    [Test]
    public void ShallNotSaveChecksum_WhenRegistrationReturnsFalse()
    {
        var checksumStoreMock = new Mock<IRegistrationChecksumStore>();
        checksumStoreMock.Setup(s => s.Get("test", null)).Returns((string?)null);

        _apiCommunicationHandlerMock.Setup(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()))
            .ReturnsAsync(false);
        _apiCommunicationHandlerMock.Setup(a => a.RequestConfiguration()).ReturnsAsync([]);

        var source = CreateSource();
        var provider = CreateProvider(source, _apiCommunicationHandlerMock, _settingStatusMonitorMock, registrationChecksumStore: checksumStoreMock.Object);

        try
        {
            provider.Load();
            _apiCommunicationHandlerMock.Verify(a => a.RegisterWithFigApi(It.IsAny<SettingsClientDefinitionDataContract>()), Times.Once);
            checksumStoreMock.Verify(s => s.Save(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>()), Times.Never);
        }
        finally
        {
            provider.Dispose();
        }
    }

    private bool VerifyDefinition(SettingsClientDefinitionDataContract definition)
    {
        return definition.Settings.Count == 14;
    }

    private TestableConfigurationSource CreateSource()
    {
        return CreateSource(_apiCommunicationHandlerMock, _settingStatusMonitorMock);
    }

    private static TestableConfigurationSource CreateSource(
        Mock<IApiCommunicationHandler> apiCommunicationHandlerMock,
        Mock<ISettingStatusMonitor> settingStatusMonitorMock)
    {
        return new TestableConfigurationSource(apiCommunicationHandlerMock, settingStatusMonitorMock)
        {
            ApiUris = ["x"],
            PollIntervalMs = 30000,
            LiveReload = false,
            Instance = null,
            ClientName = "test",
            AllowOfflineSettings = false,
            SettingsType = typeof(AllSettingsAndTypes),
            ClientSecretProviders = [new InCodeClientSecretProvider(Mock.Of<ILogger<InCodeClientSecretProvider>>(), Guid.NewGuid().ToString())]
        };
    }

    private static FigConfigurationProvider CreateProvider(
        TestableConfigurationSource source,
        Mock<IApiCommunicationHandler> apiCommunicationHandlerMock,
        Mock<ISettingStatusMonitor> settingStatusMonitorMock,
        IOfflineSettingsManager? offlineSettingsManager = null,
        ILogger<FigConfigurationProvider>? logger = null,
        IRegistrationChecksumStore? registrationChecksumStore = null)
    {
        return new FigConfigurationProvider(
            source,
            logger ?? Mock.Of<ILogger<FigConfigurationProvider>>(),
            new IpAddressResolver(),
            offlineSettingsManager ?? Mock.Of<IOfflineSettingsManager>(),
            registrationChecksumStore ?? Mock.Of<IRegistrationChecksumStore>(),
            settingStatusMonitorMock.Object,
            new AllSettingsAndTypes(),
            apiCommunicationHandlerMock.Object,
            new FigClientBridgeOptions(TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(30)));
    }

    private static SettingDataContract CreateStringSetting(string name, string value) =>
        new(name, new StringSettingDataContract(value));

    private void AssertStringCollectionWasCorrect(AllSettingsAndTypes clientOptions)
    {
        var iOptionsStringCollection = GetPropertyValue(clientOptions, nameof(AllSettingsAndTypes.StringCollectionSetting));

        var expectedStringCollection = new List<string> { "T-Rex", "Raptor" };

        Assert.That(expectedStringCollection, Is.EquivalentTo((iOptionsStringCollection as List<string>) ?? new List<string>()));
    }

    private void AssertObjectListWasCorrect(AllSettingsAndTypes clientOptions)
    {
        var iOptionsObjectList = GetPropertyValue(clientOptions, nameof(AllSettingsAndTypes.ObjectListSetting));

        var expectedObjectList = new List<SomeSetting>
        {
            new() { Key = "Ted", Value = "Doctor", MyInt = 50 },
            new() { Key = "Jill", Value = "Engineer", MyInt = 55 }
        };

        Assert.That(expectedObjectList, Is.EquivalentTo((iOptionsObjectList as List<SomeSetting>) ?? new List<SomeSetting>()));
    }

    private List<SettingDataContract> MockApiResponse(IFigConfigurationSource source)
    {
        var result = new List<SettingDataContract>
        {
            new(nameof(AllSettingsAndTypes.StringSetting), new StringSettingDataContract("SomeValue")),
            new(nameof(AllSettingsAndTypes.IntSetting), new IntSettingDataContract(55)),
            new(nameof(AllSettingsAndTypes.LongSetting), new LongSettingDataContract(66)),
            new(nameof(AllSettingsAndTypes.DoubleSetting), new DoubleSettingDataContract(22.3)),
            new(nameof(AllSettingsAndTypes.DateTimeSetting), new DateTimeSettingDataContract(new DateTime(2000, 1, 1, 4, 4, 4))),
            new(nameof(AllSettingsAndTypes.TimespanSetting), new TimeSpanSettingDataContract(TimeSpan.FromSeconds(654))),
            new(nameof(AllSettingsAndTypes.BoolSetting), new BoolSettingDataContract(true)),
            new(nameof(AllSettingsAndTypes.LookupTableSetting), new LongSettingDataContract(88)),
            new(nameof(AllSettingsAndTypes.SecretSetting), new StringSettingDataContract("verySecret")),
            new(nameof(AllSettingsAndTypes.StringCollectionSetting), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    {"Values", "T-Rex"},
                },
                new()
                {
                    {"Values", "Raptor"},
                }
            })),
            new(nameof(AllSettingsAndTypes.ObjectListSetting), new DataGridSettingDataContract(new List<Dictionary<string, object?>>()
            {
                new()
                {
                    {"Key", "Ted"},
                    {"MyInt", 50L },
                    {"Value", "Doctor" },
                },
                new()
                {
                    {"Key", "Jill"},
                    {"MyInt", 55L },
                    {"Value", "Engineer" },
                }
            })),
            new(nameof(AllSettingsAndTypes.EnumSetting), new StringSettingDataContract("Dog")),
        };

        _apiCommunicationHandlerMock.Setup(a => a.RequestConfiguration()).ReturnsAsync(result);

        return result;
    }

    private object GetValue(SettingValueBaseDataContract value)
    {
        if (value.GetValue() as string == "Dog")
            return Pets.Dog;

        return value.GetValue()!;
    }

    private static object GetPropertyValue(object obj, string propertyName)
    {
        return obj.GetType().GetProperty(propertyName)!.GetValue(obj)!;
    }
}

public class TestableConfigurationSource : FigConfigurationSource
{
    private readonly Mock<IApiCommunicationHandler> _apiCommunicationHandlerMock;
    private readonly Mock<ISettingStatusMonitor> _settingStatusMonitorMock;
    private readonly Mock<IRegistrationChecksumStore> _registrationChecksumStoreMock = new();

    public TestableConfigurationSource(Mock<IApiCommunicationHandler> apiCommunicationHandlerMock, Mock<ISettingStatusMonitor> settingStatusMonitorMock)
    {
        _apiCommunicationHandlerMock = apiCommunicationHandlerMock;
        _settingStatusMonitorMock = settingStatusMonitorMock;
        _registrationChecksumStoreMock.Setup(s => s.Get(It.IsAny<string>(), It.IsAny<string?>())).Returns((string?)null);
    }

    public Mock<IRegistrationChecksumStore> RegistrationChecksumStoreMock => _registrationChecksumStoreMock;
    
    protected override IApiCommunicationHandler CreateCommunicationHandler(HttpClient httpClient, IClientSecretProvider clientSecretProvider)
    {
        return _apiCommunicationHandlerMock.Object;
    }

    protected override ISettingStatusMonitor CreateStatusMonitor(IIpAddressResolver ipAddressResolver, IClientSecretProvider clientSecretProvider, HttpClient httpClient)
    {
        return _settingStatusMonitorMock.Object;
    }

    protected override HttpClient CreateHttpClient(bool hasOfflineSettings)
    {
        return new HttpClient();
    }

    protected override IRegistrationChecksumStore CreateRegistrationChecksumStore() =>
        _registrationChecksumStoreMock.Object;
}

public class SettingsWithPreviewMigration : SettingsBase
{
    public override string ClientDescription => "Test settings";

    [Setting("Renamed setting")]
    [MigrateFrom("OldSetting", nameof(MigrateOldSetting))]
    public string NewSetting { get; set; } = "new";

    public static string MigrateOldSetting(string oldValue) => oldValue;

    public override IEnumerable<string> GetValidationErrors() => [];
}

public class SettingsWithThrowingPreviewMigration : SettingsBase
{
    public override string ClientDescription => "Test settings";

    [Setting("Renamed setting")]
    [MigrateFrom("OldSetting", nameof(MigrateOldSetting))]
    public string NewSetting { get; set; } = "new";

    public static string MigrateOldSetting(string oldValue) =>
        throw new InvalidOperationException("Local migration failed");

    public override IEnumerable<string> GetValidationErrors() => [];
}
