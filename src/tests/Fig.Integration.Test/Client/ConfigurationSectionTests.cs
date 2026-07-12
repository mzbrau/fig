using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Fig.Client;
using Fig.Client.ExtensionMethods;
using Fig.Client.Testing.Extensions;
using Fig.Client.Testing.Integration;
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
        using var context = InitializeAndGetSerilogSettings<ClientWithConfigurationSections>(secret, offlineHttpClient);

        AssertDefaultValues(context.Options);
    }
    
    [Test]
    public void ShallApplyConfigurationSectionsWhenFigIsOnline()
    {
        var secret = GetNewSecret();
        using var context = InitializeAndGetSerilogSettings<ClientWithConfigurationSections>(secret);

        AssertDefaultValues(context.Options);
    }
    
    [Test]
    public async Task ShallApplyConfigurationSectionsWhenValuesHaveBeenUpdated()
    {
        var secret = GetNewSecret();
        using var context = InitializeAndGetSerilogSettings<ClientWithConfigurationSections>(secret);

        var settings = new List<SettingDataContract>()
        {
            new(nameof(ClientWithConfigurationSections.SerilogOverrideGoogle),
                new StringSettingDataContract("SerilogOverrideGoogleValueUpdated")),
            new(nameof(ClientWithConfigurationSections.SerilogOverrideValueGoogle),
                new StringSettingDataContract("SerilogOverrideValueGoogleValueUpdated")),
        };

        await SetSettings("ClientWithConfigurationSections", settings);
        
        context.Configuration.Reload();
        
        Assert.That(context.Options.CurrentValue.Stuff, Is.EqualTo("SerilogStuffValue"));
        Assert.That(context.Options.CurrentValue.Override, Is.Not.Null);
        Assert.That(context.Options.CurrentValue.Override?.Amazon, Is.EqualTo("SerilogOverrideAmazonValue"));
        Assert.That(context.Options.CurrentValue.Override?.Google, Is.EqualTo("SerilogOverrideGoogleValueUpdated"));
        Assert.That(context.Options.CurrentValue.Override?.Microsoft, Is.EqualTo("SerilogOverrideMicrosoftValue"));
        Assert.That(context.Options.CurrentValue.Override?.Value, Is.Not.Null);
        Assert.That(context.Options.CurrentValue.Override?.Value?.Google, Is.EqualTo("SerilogOverrideValueGoogleValueUpdated"));
    }

    [Test]
    public void ReloadableConfigurationProviderShallRespectConfigurationSections()
    {
        var settings = new ClientWithConfigurationSections();
        var configReloader = new ConfigReloader<SettingsBase>();
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

    private SerilogSettingsContext InitializeAndGetSerilogSettings<T>(string clientSecret, HttpClient? httpClientOverride = null) where T : TestSettingsBase
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
        return new SerilogSettingsContext(app, configuration, options);
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

    private sealed class SerilogSettingsContext : IDisposable
    {
        public SerilogSettingsContext(
            WebApplication application,
            IConfigurationRoot configuration,
            IOptionsMonitor<SerilogFakeSettings> options)
        {
            Application = application;
            Configuration = configuration;
            Options = options;
        }

        public WebApplication Application { get; }

        public IConfigurationRoot Configuration { get; }

        public IOptionsMonitor<SerilogFakeSettings> Options { get; }

        public void Dispose()
        {
            Application.StopAsync().GetAwaiter().GetResult();
            Application.DisposeAsync().AsTask().GetAwaiter().GetResult();
            (Configuration as IDisposable)?.Dispose();
        }
    }
}
