using Fig.Client;
using Fig.Client.ExtensionMethods;
using Fig.Client.IntegrationTest;
using Fig.Test.Common.TestSettings;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using NUnit.Framework;

namespace Fig.Integration.Test.Client;

[TestFixture]
public class NestedSettingClientTests
{
    [Test]
    public void ReloadableConfigurationProviderShallWorkWithNestedSettings()
    {
        var settings = new ClientWithNestedSettings
        {
            Database = new Database()
        };
        var configReloader = new ConfigReloader<SettingsBase>();
        var builder = WebApplication.CreateBuilder();

        var configuration = new ConfigurationBuilder()
            .AddIntegrationTestConfiguration(configReloader, settings)
            .Build();
        
        builder.Services.Configure<ClientWithNestedSettings>(configuration);
        var app = builder.Build();

        var options = app.Services.GetRequiredService<IOptionsMonitor<ClientWithNestedSettings>>();

        settings.MessageBus = new MessageBus()
        {
            Uri = "some uri",
            Auth = new Authorization()
            {
                Username = "user",
                Password = "pass"
            },
        };
        
        configReloader.Reload(settings);
        
        Assert.That(options.CurrentValue?.MessageBus?.Uri, Is.EqualTo(settings.MessageBus.Uri));
        Assert.That(options.CurrentValue?.MessageBus?.Auth?.Username, Is.EqualTo(settings.MessageBus.Auth.Username));
        Assert.That(options.CurrentValue?.MessageBus?.Auth?.Password, Is.EqualTo(settings.MessageBus.Auth.Password));
    }
}