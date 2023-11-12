using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Client.ExtensionMethods;
using Fig.Client.IntegrationTest;
using Fig.Contracts.Settings;
using Fig.Test.Common;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

public class ConfigurationSectionTests : IntegrationTestBase
{
    [Test]
    public void ShallApplyConfigurationSectionsWhenFigIsOfflineAndNoBackup()
    {
        var secret = GetNewSecret();
        var offlineHttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:9999")
        };
        var (options, _) = InitializeAndGetSerilogSettings<ClientWithConfigurationSections>(secret, offlineHttpClient);
        
        AssertDefaultValues(options);
    }
    
    [Test]
    public void ShallApplyConfigurationSectionsWhenFigIsOnline()
    {
        var secret = GetNewSecret();
        var (options, _) = InitializeAndGetSerilogSettings<ClientWithConfigurationSections>(secret);
        
        AssertDefaultValues(options);
    }
    
    [Test]
    public async Task ShallApplyConfigurationSectionsWhenValuesHaveBeenUpdated()
    {
        var secret = GetNewSecret();
        var (options, configuration) = InitializeAndGetSerilogSettings<ClientWithConfigurationSections>(secret);

        var settings = new List<SettingDataContract>()
        {
            new(nameof(ClientWithConfigurationSections.SerilogOverrideGoogle),
                new StringSettingDataContract("SerilogOverrideGoogleValueUpdated")),
            new(nameof(ClientWithConfigurationSections.SerilogOverrideValueGoogle),
                new StringSettingDataContract("SerilogOverrideValueGoogleValueUpdated")),
        };

        await SetSettings("ClientWithConfigurationSections", settings);
        
        configuration.Reload();
        
        Assert.That(options.CurrentValue.Stuff, Is.EqualTo("SerilogStuffValue"));
        Assert.That(options.CurrentValue.Override, Is.Not.Null);
        Assert.That(options.CurrentValue.Override?.Amazon, Is.EqualTo("SerilogOverrideAmazonValue"));
        Assert.That(options.CurrentValue.Override?.Google, Is.EqualTo("SerilogOverrideGoogleValueUpdated"));
        Assert.That(options.CurrentValue.Override?.Microsoft, Is.EqualTo("SerilogOverrideMicrosoftValue"));
        Assert.That(options.CurrentValue.Override?.Value, Is.Not.Null);
        Assert.That(options.CurrentValue.Override?.Value?.Google, Is.EqualTo("SerilogOverrideValueGoogleValueUpdated"));
    }

    [Test]
    public void ReloadableConfigurationProviderShallRespectConfigurationSections()
    {
        var settings = new ClientWithConfigurationSections();
        var configReloader = new ConfigReloader();
        var builder = WebApplication.CreateBuilder();

        var configuration = new ConfigurationBuilder()
            .AddIntegrationTestConfiguration(configReloader, settings)
            .Build();

        var serilogSection = configuration.GetSection("Serilog");
        builder.Services.Configure<SerilogFakeSettings>(serilogSection);
        var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptionsMonitor<SerilogFakeSettings>>();
        
        AssertDefaultValues(options);
    }

    private (IOptionsMonitor<SerilogFakeSettings> settings, IConfigurationRoot configuration) InitializeAndGetSerilogSettings<T>(string clientSecret, HttpClient? httpClientOverride = null) where T : TestSettingsBase
    {
        var builder = WebApplication.CreateBuilder();
        var settings = Activator.CreateInstance<T>();

        var configuration = new ConfigurationBuilder()
            .AddFig<T>(o =>
            {
                o.ClientName = settings.ClientName;
                o.HttpClient = httpClientOverride ?? GetHttpClient();
                o.ClientSecretOverride = clientSecret;
            }).Build();

        var serilogSection = configuration.GetSection("Serilog");
        builder.Services.Configure<SerilogFakeSettings>(serilogSection);
        
        var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptionsMonitor<SerilogFakeSettings>>();
        return (options, configuration);
    }

    private void AssertDefaultValues(IOptionsMonitor<SerilogFakeSettings> options)
    {
        Assert.That(options.CurrentValue.Stuff, Is.EqualTo("SerilogStuffValue"));
        Assert.That(options.CurrentValue.Override, Is.Not.Null);
        Assert.That(options.CurrentValue.Override?.Amazon, Is.EqualTo("SerilogOverrideAmazonValue"));
        Assert.That(options.CurrentValue.Override?.Google, Is.EqualTo("SerilogOverrideGoogleValue"));
        Assert.That(options.CurrentValue.Override?.Microsoft, Is.EqualTo("SerilogOverrideMicrosoftValue"));
        Assert.That(options.CurrentValue.Override?.Value, Is.Not.Null);
        Assert.That(options.CurrentValue.Override?.Value?.Google, Is.EqualTo("SerilogOverrideValueGoogleValue"));
    }
}