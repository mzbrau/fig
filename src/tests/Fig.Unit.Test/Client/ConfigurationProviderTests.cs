using System.Collections.Generic;
using Fig.Test.Common.TestSettings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using Fig.Common.NetStandard.IpAddress;
using System.Net.Http;
using Fig.Contracts.Settings;
using Moq;
using Fig.Client.Status;
using System;
using System.Linq;
using Fig.Client.ClientSecret;
using Fig.Contracts.SettingDefinitions;
using Fig.Client.ConfigurationProvider;
using Fig.Client.Contracts;
using Microsoft.Extensions.Logging;

namespace Fig.Unit.Test.Client;

[TestFixture]
public class ConfigurationProviderTests
{
    private readonly Mock<IApiCommunicationHandler> _apiCommunicationHandlerMock = new();
    private readonly Mock<ISettingStatusMonitor> _settingStatusMonitorMock = new();

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

        var builder = new ConfigurationBuilder();
        builder.Add(source);
        builder.Build();

        _apiCommunicationHandlerMock.Verify(a => a.RegisterWithFigApi(It.Is<SettingsClientDefinitionDataContract>(definition => VerifyDefinition(definition))));
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

    private bool VerifyDefinition(SettingsClientDefinitionDataContract definition)
    {
        return definition.Settings.Count == 14;
    }

    private TestableConfigurationSource CreateSource()
    {
        return new TestableConfigurationSource(_apiCommunicationHandlerMock, _settingStatusMonitorMock)
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

    public TestableConfigurationSource(Mock<IApiCommunicationHandler> apiCommunicationHandlerMock, Mock<ISettingStatusMonitor> settingStatusMonitorMock)
    {
        _apiCommunicationHandlerMock = apiCommunicationHandlerMock;
        _settingStatusMonitorMock = settingStatusMonitorMock;
    }
    
    protected override IApiCommunicationHandler CreateCommunicationHandler(HttpClient httpClient, IIpAddressResolver ipAddressResolver, IClientSecretProvider clientSecretProvider)
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
}